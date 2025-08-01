using backend_manage.Entities;
using backend_manage.DTOs;
using backend_manage.Repositories;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using AutoMapper;
using System.Text.Json;
using System.Linq;
using backend_manage.Repositories.Interfaces;

namespace backend_manage.Services.AuthService.Helpers;

public class StudentExamSessionCacheHelper
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<StudentExamSessionCacheHelper> _logger;
    private readonly IRepository<ExamSessionSubject> _examSessionSubjectRepository;
    private readonly IRepository<ExamRoom> _examRoomRepository;
    private readonly IRepository<StudentExamSession> _studentExamSessionRepository;
    private readonly IRepository<Student> _studentRepository;
    private readonly IMapper _mapper;

    public StudentExamSessionCacheHelper(
        IConnectionMultiplexer redis, 
        ILogger<StudentExamSessionCacheHelper> logger,
        IRepository<ExamSessionSubject> examSessionSubjectRepository,
        IRepository<ExamRoom> examRoomRepository,
        IRepository<StudentExamSession> studentExamSessionRepository,
        IRepository<Student> studentRepository,
        IMapper mapper)
    {
        _redis = redis;
        _logger = logger;
        _examSessionSubjectRepository = examSessionSubjectRepository;
        _examRoomRepository = examRoomRepository;
        _studentExamSessionRepository = studentExamSessionRepository;
        _studentRepository = studentRepository;
        _mapper = mapper;
    }

    #region GetStudentExamSessionsFromRedisAsync
    public async Task<IEnumerable<StudentExamSessionDto>?> GetStudentExamSessionsFromRedisAsync(string studentCode)
    {
        var (redisAvailable, cacheSessions) = await GetStudentExamSessionsFromCache(studentCode);
        if (cacheSessions != null)
        {
            _logger.LogInformation("Tìm thấy danh sách các phiên thi của sinh viên: {key}", studentCode);
            var result = cacheSessions.Select(x => _mapper.Map<StudentExamSessionDto>(x)).ToList();
            return result;
        }
        // Redis có cacheSessions "null" → vẫn truy vấn DB, KHÔNG return null ở đây nữa
        // B2: Fallback DB
        if (!redisAvailable)
        {
            _logger.LogInformation("Tìm kiếm sinh viên ở db vì không kết nối được với redis {key}", studentCode);
        }

        var dbSessions = await GetStudentExamSessionsFromDb(studentCode);
        
        if (redisAvailable)
            await CacheStudentExamSessions(studentCode, dbSessions);
        
        var results = dbSessions.Select(x => _mapper.Map<StudentExamSessionDto>(x)).ToList();
        return results;
    }

    public async Task<IEnumerable<StudentExamSession>?> GetStudentExamSessionsFromDb(string studentCode)
    {
        var student = await _studentRepository.GetQueryable()
            .FirstOrDefaultAsync(x => x.StudentCode == studentCode);

        if (student == null)
        {
            _logger.LogWarning("Không tìm thấy sinh viên với mã {StudentCode}", studentCode);
            return null;
        }

        var sessions = await _studentExamSessionRepository.GetQueryable()
            .Where(x => x.StudentId == student.StudentId && x.IsCompleted == false)
            .Include(x => x.ExamSessionSubject)
            .ThenInclude(x => x.Subject)
            .Include(x => x.ExamRoom)
            .ToListAsync();

        if(sessions.Any())
        {
            _logger.LogInformation("Tìm thấy phiên thi của sinh viên {StudentCode} từ database", studentCode);
        }
        else
        {
            _logger.LogInformation("Không tìm thấy phiên thi nào cho sinh viên {StudentCode} từ database", studentCode);
        }

      
        return sessions;
    }

    
    private async Task<(bool redisAvailable, List<StudentExamSessionCacheDto>? sessions)> GetStudentExamSessionsFromCache(string studentCode)
    {
        try
        {
            var db = _redis.GetDatabase();
            string redisHashKey = $"student_exam_sessions:{studentCode}";

            var hashEntries = await db.HashGetAllAsync(redisHashKey);

            if (hashEntries.Length == 0)
            {
                _logger.LogInformation("Redis không có phiên thi cho sinh viên: {StudentCode}", studentCode);
                return (true, new List<StudentExamSessionCacheDto>()); // Redis hoạt động nhưng không có dữ liệu
            }

            var sessions = new List<StudentExamSessionCacheDto>();

            foreach (var entry in hashEntries)
            {
                try
                {
                    var session = JsonSerializer.Deserialize<StudentExamSessionCacheDto>(entry.Value);
                    if (session != null)
                    {
                        sessions.Add(session);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi deserialize phiên thi từ Redis cho sinh viên {StudentCode}, key = {SessionId}",
                        studentCode, entry.Name.ToString());
                }
            }

            _logger.LogDebug("Lấy {Count} phiên thi từ Redis cho sinh viên {StudentCode}", sessions.Count, studentCode);
            return (true, sessions); // Redis OK
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis lỗi khi lấy phiên thi cho sinh viên {StudentCode}", studentCode);
            return (false, null); // Redis lỗi
        }
    }


    
    public async Task CacheStudentExamSessions(string studentCode, IEnumerable<StudentExamSession> sessions)
    {
        try
        {
            var db = _redis.GetDatabase();
            string redisHashKey = $"student_exam_sessions:{studentCode}";
            int cachedCount = 0;

            var hashEntries = new List<HashEntry>();

            foreach (var session in sessions)
            {
                try
                {
                    var cacheDto = await ConvertToCacheDtoAsync(session);
                    var sessionJson = JsonSerializer.Serialize(cacheDto);
                    hashEntries.Add(new HashEntry(session.StudentExamSessionId.ToString(), sessionJson));
                    cachedCount++;
                    _logger.LogInformation("Đã cache phiên thi cho sinh viên: {StudentCode}", studentCode);

                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi chuyển session {SessionId} cho sinh viên {StudentCode}", 
                        session.StudentExamSessionId, studentCode);
                }
            }

            if (hashEntries.Any())
            {
                await db.HashSetAsync(redisHashKey, hashEntries.ToArray());
                // Có thể thêm TTL cho toàn bộ hash (nếu muốn)
                await db.KeyExpireAsync(redisHashKey, TimeSpan.FromHours(6));
            }

            _logger.LogDebug("Đã cache {CachedCount} phiên thi (HASH) cho sinh viên {StudentCode}", cachedCount, studentCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cache phiên thi (HASH) cho sinh viên {StudentCode}", studentCode);
        }
    }
    
    private async Task<StudentExamSessionCacheDto> ConvertToCacheDtoAsync(StudentExamSession session)
    {
        // Map cơ bản từ entity sang DTO
        var cacheDto = _mapper.Map<StudentExamSessionCacheDto>(session);

        // Nếu thiếu SubjectName hoặc Duration, thì lấy lại từ DB
        if (string.IsNullOrWhiteSpace(cacheDto.SubjectName) || cacheDto.Duration <= 0)
        {
            var examSessionSubject = await _examSessionSubjectRepository.GetQueryable()
                .Include(ess => ess.Subject)
                .FirstOrDefaultAsync(ess => ess.ExamSessionSubjectId == session.ExamSessionSubjectId);

            cacheDto.SubjectName = examSessionSubject?.Subject?.SubjectName ?? string.Empty;
            cacheDto.Duration = examSessionSubject?.Duration ?? 0;
        }

        // Nếu thiếu RoomName, truy vấn lại ExamRoom
        if (string.IsNullOrWhiteSpace(cacheDto.RoomName) && session.ExamRoomId.HasValue)
        {
            var examRoom = await _examRoomRepository.GetQueryable()
                .FirstOrDefaultAsync(er => er.ExamRoomId == session.ExamRoomId.Value);

            cacheDto.RoomName = examRoom?.RoomName ?? string.Empty;
        }

        return cacheDto;
    }
    #endregion
    
    
    
    
    
    
    
    
    
     
    private async Task<(bool Success, string Message)> UpdateStudentAnswersInDatabaseAsync(
        string studentCode, int shuffledExamPaperId, string newAnswersString)
    {
        try
        {
            var student = await _studentRepository.GetQueryable().FirstOrDefaultAsync(x => x.StudentCode == studentCode);
            if (student == null)
            {
                _logger.LogWarning("Không tìm thấy sinh viên với mã {StudentCode}", studentCode);
                return (false, "Không tìm thấy sinh viên");
            }

            var session = await _studentExamSessionRepository.GetQueryable()
                .Where(x => x.StudentId == student.StudentId && x.ShuffledExamPaperId == shuffledExamPaperId)
                .FirstOrDefaultAsync();

            if (session != null)
            {
                session.StudentAnswersString = newAnswersString;
                session.UpdatedAt = DateTime.UtcNow;
                
                await _studentExamSessionRepository.UpdateAsync(session);
                
                _logger.LogInformation("Đã cập nhật StudentAnswersString trong database cho sinh viên {StudentCode} với ShuffledExamPaperId {ShuffledExamPaperId}", 
                    studentCode, shuffledExamPaperId);
                
                return (true, "Cập nhật đáp án thành công");
            }

            _logger.LogWarning("Không tìm thấy session trong database cho sinh viên {StudentCode} với ShuffledExamPaperId {ShuffledExamPaperId}", 
                studentCode, shuffledExamPaperId);
            return (false, "Không tìm thấy phiên thi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật StudentAnswersString trong database cho sinh viên {StudentCode}", studentCode);
            return (false, "Lỗi khi cập nhật đáp án");
        }
    }


    public async Task<(bool Success, string Message, string? NewAnswersString)> GetAndUpdateStudentAnswersAsync(
        string studentCode, int shuffledExamPaperId, int index, string answer)
    {
        try
        {
            var db = _redis.GetDatabase();
            string sessionCacheKey = $"student_exam_session:{studentCode}:*";
            
            // Tìm session có ShuffledExamPaperId tương ứng
            var keys = db.Multiplexer.GetServer(db.Multiplexer.GetEndPoints().First()).Keys(pattern: sessionCacheKey);
            
            foreach (var key in keys)
            {
                try
                {
                    var sessionData = await db.StringGetAsync(key);
                    if (sessionData.HasValue)
                    {
                        var cachedSession = JsonSerializer.Deserialize<StudentExamSessionCacheDto>(sessionData);
                        if (cachedSession?.ShuffledExamPaperId == shuffledExamPaperId)
                        {
                            // Lấy đáp án hiện tại
                            var currentAnswersString = cachedSession.StudentAnswersString ?? "";
                            
                            if (string.IsNullOrEmpty(currentAnswersString))
                            {
                                _logger.LogError("Không tìm thấy chuỗi đáp án cho sinh viên {StudentCode} với ShuffledExamPaperId {ShuffledExamPaperId}", 
                                    studentCode, shuffledExamPaperId);
                                return (false, "Không tìm thấy bài thi của sinh viên", null);
                            }

                            // Tách chuỗi đáp án thành mảng
                            var answerParts = currentAnswersString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                            var updatedParts = new List<string>();

                            // Cập nhật đáp án tại index tương ứng
                            bool found = false;
                            foreach (var part in answerParts)
                            {
                                if (part.StartsWith($"({index},"))
                                {
                                    updatedParts.Add($"({index},{answer})");
                                    found = true;
                                }
                                else if (!string.IsNullOrWhiteSpace(part))
                                {
                                    updatedParts.Add(part);
                                }
                            }

                            // Nếu không tìm thấy index, thêm mới
                            if (!found)
                            {
                                updatedParts.Add($"({index},{answer})");
                            }

                            // Tạo chuỗi đáp án mới
                            string newAnswersString = string.Join(";", updatedParts) + ";";

                            // Cập nhật session trong cache
                            cachedSession.StudentAnswersString = newAnswersString;
                            cachedSession.UpdatedAt = DateTime.UtcNow;
                            
                            // Lưu lại vào cache
                            var updatedSessionJson = JsonSerializer.Serialize(cachedSession);
                            await db.StringSetAsync(key, updatedSessionJson, TimeSpan.FromHours(6));
                            
                            _logger.LogInformation("Đã cập nhật đáp án trong cache cho session {StudentCode}:{SessionId}. Key: {Key}", 
                                studentCode, cachedSession.StudentExamSessionId, key);
                            
                            return (true, "Cập nhật đáp án thành công", newAnswersString);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi cập nhật session trong cache với key: {Key}", key);
                }
            }
            
            _logger.LogWarning("Không tìm thấy session với ShuffledExamPaperId {ShuffledExamPaperId} cho sinh viên {StudentCode} trong cache, thử database", 
                shuffledExamPaperId, studentCode);
            
            // Fallback: Cập nhật trong database nếu không có trong cache
            return await UpdateStudentAnswersInDatabaseAsync(studentCode, shuffledExamPaperId, index, answer);
        }
        catch (RedisTimeoutException ex)
        {
            _logger.LogWarning(ex, "Redis timeout khi cập nhật đáp án cho sinh viên {StudentCode}, chuyển sang database", studentCode);
            return await UpdateStudentAnswersInDatabaseAsync(studentCode, shuffledExamPaperId, index, answer);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis connection error khi cập nhật đáp án cho sinh viên {StudentCode}, chuyển sang database", studentCode);
            return await UpdateStudentAnswersInDatabaseAsync(studentCode, shuffledExamPaperId, index, answer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật đáp án cho sinh viên {StudentCode}", studentCode);
            return await UpdateStudentAnswersInDatabaseAsync(studentCode, shuffledExamPaperId, index, answer);
        }
    }

    /// <summary>
    /// Fallback: Cập nhật đáp án trong database khi Redis không khả dụng
    /// </summary>
    private async Task<(bool Success, string Message, string? NewAnswersString)> UpdateStudentAnswersInDatabaseAsync(
        string studentCode, int shuffledExamPaperId, int index, string answer)
    {
        try
        {
            var student = await _studentRepository.GetQueryable().FirstOrDefaultAsync(x => x.StudentCode == studentCode);
            if (student == null)
            {
                _logger.LogWarning("Không tìm thấy sinh viên với mã {StudentCode}", studentCode);
                return (false, "Không tìm thấy sinh viên", null);
            }

            var session = await _studentExamSessionRepository.GetQueryable()
                .Where(x => x.StudentId == student.StudentId && x.ShuffledExamPaperId == shuffledExamPaperId)
                .FirstOrDefaultAsync();

            if (session != null)
            {
                // Lấy đáp án hiện tại từ database
                var currentAnswersString = session.StudentAnswersString ?? "";
                
                if (string.IsNullOrEmpty(currentAnswersString))
                {
                    _logger.LogError("Không tìm thấy chuỗi đáp án cho sinh viên {StudentCode} với ShuffledExamPaperId {ShuffledExamPaperId}", 
                        studentCode, shuffledExamPaperId);
                    return (false, "Không tìm thấy bài thi của sinh viên", null);
                }

                // Tách chuỗi đáp án thành mảng
                var answerParts = currentAnswersString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var updatedParts = new List<string>();

                // Cập nhật đáp án tại index tương ứng
                bool found = false;
                foreach (var part in answerParts)
                {
                    if (part.StartsWith($"({index},"))
                    {
                        updatedParts.Add($"({index},{answer})");
                        found = true;
                    }
                    else if (!string.IsNullOrWhiteSpace(part))
                    {
                        updatedParts.Add(part);
                    }
                }

                // Nếu không tìm thấy index, thêm mới
                if (!found)
                {
                    updatedParts.Add($"({index},{answer})");
                }

                // Tạo chuỗi đáp án mới
                string newAnswersString = string.Join(";", updatedParts) + ";";

                // Cập nhật trong database
                session.StudentAnswersString = newAnswersString;
                session.UpdatedAt = DateTime.UtcNow;
                
                await _studentExamSessionRepository.UpdateAsync(session);
                
                _logger.LogInformation("Đã cập nhật đáp án trong database cho sinh viên {StudentCode} với ShuffledExamPaperId {ShuffledExamPaperId}", 
                    studentCode, shuffledExamPaperId);
                
                return (true, "Cập nhật đáp án thành công", newAnswersString);
            }

            _logger.LogWarning("Không tìm thấy session trong database cho sinh viên {StudentCode} với ShuffledExamPaperId {ShuffledExamPaperId}", 
                studentCode, shuffledExamPaperId);
            return (false, "Không tìm thấy phiên thi", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật đáp án trong database cho sinh viên {StudentCode}", studentCode);
            return (false, "Lỗi khi cập nhật đáp án", null);
        }
    }


    public async Task<string?> GetStudentAnswersFromCacheAsync(string studentCode, int shuffledExamPaperId)
    {
        try
        {
            var db = _redis.GetDatabase();
            string sessionCacheKey = $"student_exam_session:{studentCode}:*";
            
            var keys = db.Multiplexer.GetServer(db.Multiplexer.GetEndPoints().First()).Keys(pattern: sessionCacheKey);
            
            foreach (var key in keys)
            {
                try
                {
                    var sessionData = await db.StringGetAsync(key);
                    if (sessionData.HasValue)
                    {
                        var cachedSession = JsonSerializer.Deserialize<StudentExamSessionCacheDto>(sessionData);
                        if (cachedSession?.ShuffledExamPaperId == shuffledExamPaperId)
                        {
                            _logger.LogDebug("Đã lấy StudentAnswersString từ cache cho session {StudentCode}:{SessionId}", 
                                studentCode, cachedSession.StudentExamSessionId);
                            return cachedSession.StudentAnswersString;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi đọc session từ cache với key: {Key}", key);
                }
            }
            
            _logger.LogDebug("Không tìm thấy StudentAnswersString trong cache cho sinh viên {StudentCode} với ShuffledExamPaperId {ShuffledExamPaperId}, thử từ database", 
                studentCode, shuffledExamPaperId);
            
            // Fallback: Lấy từ database nếu không có trong cache
            return await GetStudentAnswersFromDatabaseAsync(studentCode, shuffledExamPaperId);
        }
        catch (RedisTimeoutException ex)
        {
            _logger.LogWarning(ex, "Redis timeout khi lấy StudentAnswersString từ cache cho sinh viên {StudentCode}, chuyển sang database", studentCode);
            return await GetStudentAnswersFromDatabaseAsync(studentCode, shuffledExamPaperId);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis connection error khi lấy StudentAnswersString từ cache cho sinh viên {StudentCode}, chuyển sang database", studentCode);
            return await GetStudentAnswersFromDatabaseAsync(studentCode, shuffledExamPaperId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy StudentAnswersString từ cache cho sinh viên {StudentCode}", studentCode);
            return await GetStudentAnswersFromDatabaseAsync(studentCode, shuffledExamPaperId);
        }
    }

    /// <summary>
    /// Lấy StudentAnswersString từ database khi Redis không khả dụng
    /// </summary>
    private async Task<string?> GetStudentAnswersFromDatabaseAsync(string studentCode, int shuffledExamPaperId)
    {
        try
        {
            var student = await _studentRepository.GetQueryable().FirstOrDefaultAsync(x => x.StudentCode == studentCode);
            if (student == null)
            {
                _logger.LogWarning("Không tìm thấy sinh viên với mã {StudentCode}", studentCode);
                return null;
            }

            var session = await _studentExamSessionRepository.GetQueryable()
                .Where(x => x.StudentId == student.StudentId && x.ShuffledExamPaperId == shuffledExamPaperId)
                .FirstOrDefaultAsync();

            if (session != null)
            {
                _logger.LogInformation("Đã lấy StudentAnswersString từ database cho sinh viên {StudentCode} với ShuffledExamPaperId {ShuffledExamPaperId}", 
                    studentCode, shuffledExamPaperId);
                return session.StudentAnswersString;
            }

            _logger.LogWarning("Không tìm thấy session trong database cho sinh viên {StudentCode} với ShuffledExamPaperId {ShuffledExamPaperId}", 
                studentCode, shuffledExamPaperId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy StudentAnswersString từ database cho sinh viên {StudentCode}", studentCode);
            return null;
        }
    }
    
    
  public async Task<StudentExamSessionCacheDto?> GetStudentExamSessionFromCacheAsync(string studentCode, int studentExamSessionId)
    {
        try
        {
            var db = _redis.GetDatabase();
            string sessionCacheKey = $"student_exam_session:{studentCode}:{studentExamSessionId}";
            
            var sessionData = await db.StringGetAsync(sessionCacheKey);
            if (sessionData.HasValue)
            {
                try
                {
                    var cachedSession = JsonSerializer.Deserialize<StudentExamSessionCacheDto>(sessionData);
                    if (cachedSession != null)
                    {
                        _logger.LogDebug("Đã tìm thấy StudentExamSession trong cache: {StudentCode}:{StudentExamSessionId}", 
                            studentCode, studentExamSessionId);
                        return cachedSession;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi deserialize StudentExamSession từ cache với key: {Key}", sessionCacheKey);
                }
            }
            
            _logger.LogDebug("Không tìm thấy StudentExamSession trong cache: {StudentCode}:{StudentExamSessionId}, tìm trong database", 
                studentCode, studentExamSessionId);
            
            // Tìm trong database nếu không có trong cache
            var student = await _studentRepository.GetQueryable().FirstOrDefaultAsync(x => x.StudentCode == studentCode);
            if (student == null)
            {
                _logger.LogWarning("Không tìm thấy sinh viên với mã {StudentCode}", studentCode);
                return null;
            }
            
            var session = await _studentExamSessionRepository.GetQueryable()
                .Where(x => x.StudentId == student.StudentId && x.StudentExamSessionId == studentExamSessionId)
                .Include(x => x.ExamSessionSubject)
                    .ThenInclude(x => x.Subject)
                .Include(x => x.ExamRoom)
                .FirstOrDefaultAsync();
            
            if (session != null)
            {
                // Cache lại vào Redis (nếu Redis khả dụng)
                try
                {
                    var cacheDto = await ConvertToCacheDtoAsync(session);
                    var sessionJson = JsonSerializer.Serialize(cacheDto);
                    await db.StringSetAsync(sessionCacheKey, sessionJson, TimeSpan.FromHours(6));
                    
                    _logger.LogDebug("Đã tìm thấy StudentExamSession trong database và cache lại: {StudentCode}:{StudentExamSessionId}", 
                        studentCode, studentExamSessionId);
                    return cacheDto;
                }
                catch (Exception cacheEx)
                {
                    _logger.LogWarning(cacheEx, "Không thể cache StudentExamSession do Redis không khả dụng: {StudentCode}:{StudentExamSessionId}", 
                        studentCode, studentExamSessionId);
                    
                    // Vẫn trả về dữ liệu từ database ngay cả khi không cache được
                    var cacheDto = await ConvertToCacheDtoAsync(session);
                    return cacheDto;
                }
            }
            
            _logger.LogDebug("Không tìm thấy StudentExamSession trong database: {StudentCode}:{StudentExamSessionId}", 
                studentCode, studentExamSessionId);
            return null;
        }
        catch (Exception ex){
            _logger.LogError(ex, "Lỗi khi tìm StudentExamSession từ cache và database: {StudentCode}:{StudentExamSessionId}", 
                studentCode, studentExamSessionId);
            return null;
        }
    }
    

 
     public async Task<(bool Success, string Message)> UpdateStudentAnswersInCacheAsync(
        string studentCode, int shuffledExamPaperId, string newAnswersString)
    {
        try
        {
            var db = _redis.GetDatabase();
            string sessionCacheKey = $"student_exam_session:{studentCode}:*";
            
            // Tìm session có ShuffledExamPaperId tương ứng
            var keys = db.Multiplexer.GetServer(db.Multiplexer.GetEndPoints().First()).Keys(pattern: sessionCacheKey);
            
            foreach (var key in keys)
            {
                try
                {
                    var sessionData = await db.StringGetAsync(key);
                    if (sessionData.HasValue)
                    {
                        var cachedSession = JsonSerializer.Deserialize<StudentExamSessionCacheDto>(sessionData);
                        if (cachedSession?.ShuffledExamPaperId == shuffledExamPaperId)
                        {
                            // Cập nhật StudentAnswersString
                            cachedSession.StudentAnswersString = newAnswersString;
                            cachedSession.UpdatedAt = DateTime.UtcNow;
                            
                            // Lưu lại vào cache
                            var updatedSessionJson = JsonSerializer.Serialize(cachedSession);
                            await db.StringSetAsync(key, updatedSessionJson, TimeSpan.FromHours(6));
                            
                            _logger.LogInformation("Đã cập nhật StudentAnswersString trong cache cho session {StudentCode}:{SessionId}. Key: {Key}", 
                                studentCode, cachedSession.StudentExamSessionId, key);
                            
                            return (true, "Cập nhật đáp án thành công");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi cập nhật session trong cache với key: {Key}", key);
                }
            }
            
            _logger.LogWarning("Không tìm thấy session với ShuffledExamPaperId {ShuffledExamPaperId} cho sinh viên {StudentCode} trong cache, thử database", 
                shuffledExamPaperId, studentCode);
            
            // Fallback: Cập nhật trong database nếu không có trong cache
            return await UpdateStudentAnswersInDatabaseAsync(studentCode, shuffledExamPaperId, newAnswersString);
        }
        catch (RedisTimeoutException ex)
        {
            _logger.LogWarning(ex, "Redis timeout khi cập nhật StudentAnswersString trong cache cho sinh viên {StudentCode}, chuyển sang database", studentCode);
            return await UpdateStudentAnswersInDatabaseAsync(studentCode, shuffledExamPaperId, newAnswersString);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis connection error khi cập nhật StudentAnswersString trong cache cho sinh viên {StudentCode}, chuyển sang database", studentCode);
            return await UpdateStudentAnswersInDatabaseAsync(studentCode, shuffledExamPaperId, newAnswersString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật StudentAnswersString trong cache cho sinh viên {StudentCode}", studentCode);
            return await UpdateStudentAnswersInDatabaseAsync(studentCode, shuffledExamPaperId, newAnswersString);
        }
    }
} 