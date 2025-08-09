using System.Text.Json;
using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using backend_manage.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace backend_manage.core.Services.AuthService.Helpers;

public class StudentExamSessionCacheHelper
{
    private readonly IRedisService _redisService;
    private readonly ILogger<StudentExamSessionCacheHelper> _logger;
    private readonly IRepository<ExamSessionSubject> _examSessionSubjectRepository;
    private readonly IRepository<ExamRoom> _examRoomRepository;
    private readonly IRepository<StudentExamSession> _studentExamSessionRepository;
    private readonly IRepository<Student> _studentRepository;
    private readonly IMapper _mapper;

    public StudentExamSessionCacheHelper(
        IRedisService redisService,
        ILogger<StudentExamSessionCacheHelper> logger,
        IRepository<ExamSessionSubject> examSessionSubjectRepository,
        IRepository<ExamRoom> examRoomRepository,
        IRepository<StudentExamSession> studentExamSessionRepository,
        IRepository<Student> studentRepository,
        IMapper mapper)
    {
        _redisService = redisService;
        _logger = logger;
        _examSessionSubjectRepository = examSessionSubjectRepository;
        _examRoomRepository = examRoomRepository;
        _studentExamSessionRepository = studentExamSessionRepository;
        _studentRepository = studentRepository;
        _mapper = mapper;
    }

    
    //Hoàn thành 
    #region GetStudentExamSessionsFromRedisAsync
    
    //phương thức chính dùng cho lúc đăng nhập(lấy danh sách)
    public async Task<IEnumerable<StudentExamSessionDto>?> GetListStudentExamSessionsFromRedisAsync(string studentCode)
    {
        var (redisAvailable, cacheSessions) = await GetListStudentExamSessionsFromCache(studentCode);
        if (cacheSessions != null && cacheSessions.Any())
        {
            _logger.LogInformation("Tìm thấy danh sách các phiên thi chưa hoàn thành của sinh viên: {key} ở redis ", studentCode);
            var result = cacheSessions.Select(x => _mapper.Map<StudentExamSessionDto>(x)).ToList();
            return result;
        }
        // Redis có cacheSessions "null" → vẫn truy vấn DB, KHÔNG return null ở đây nữa
        // B2: Fallback DB
        if (!redisAvailable)
        {
            _logger.LogInformation("Tìm kiếm sinh viên ở db vì không kết nối được với redis {key}", studentCode);
        }

        var dbSessions = await GetListStudentExamSessionsFromDb(studentCode);
        
        if (redisAvailable)
            await CacheStudentExamSessions(studentCode, dbSessions);
        
        var results = dbSessions.Select(x => _mapper.Map<StudentExamSessionDto>(x)).ToList();
        return results;
    }

    public async Task<IEnumerable<StudentExamSession>?> GetListStudentExamSessionsFromDb(string studentCode)
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
            _logger.LogInformation("Tìm thấy {Count} phiên thi chưa hoàn thành của sinh viên {StudentCode} từ database", sessions.Count, studentCode);
        }
        else
        {
            _logger.LogInformation("Không tìm thấy phiên thi chưa hoàn thành nào cho sinh viên {StudentCode} từ database", studentCode);
        }

      
        return sessions;
    }
    
    private async Task<(bool redisAvailable, List<StudentExamSessionCacheDto>? sessions)> GetListStudentExamSessionsFromCache(string studentCode)
    {
        try
        {
            string redisHashKey = $"student_exam_sessions:{studentCode}";

            var hashData = await _redisService.HashGetAllAsync(redisHashKey);

            if (hashData == null)
            {
                _logger.LogInformation("Redis không có phiên thi hoặc không kết nối được: {StudentCode}", studentCode);
                return (_redisService.IsConnected, new List<StudentExamSessionCacheDto>()); 
            }

            var sessions = new List<StudentExamSessionCacheDto>();

            foreach (var kvp in hashData)
            {
                try
                {
                    var session = JsonSerializer.Deserialize<StudentExamSessionCacheDto>(kvp.Value);
                    if (session != null && !session.IsCompleted)
                    {
                        sessions.Add(session);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi deserialize phiên thi từ Redis cho sinh viên {StudentCode}, key = {SessionId}",
                        studentCode, kvp.Key);
                }
            }

            _logger.LogDebug("Lấy {Count} phiên thi chưa hoàn thành từ Redis cho sinh viên {StudentCode}", sessions.Count, studentCode);
            return (true, sessions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis lỗi khi lấy phiên thi cho sinh viên {StudentCode}", studentCode);
            return (false, null);
        }
    }

    public async Task CacheStudentExamSessions(string studentCode, IEnumerable<StudentExamSession>? sessions)
    {
        try
        {
            var db = _redisService.GetDatabase();
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
                    _logger.LogInformation("Đã cache phiên thi cho sinh viên: {StudentCode} {cachedCount}", studentCode,cachedCount);

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
    
    //phương thức chính dùng cho bắt đầu làm bài(lấy 1 phiên)
    public async Task<(bool redisAvailable, StudentExamSessionCacheDto?)> GetStudentExamSessionAsync(string studentCode, int studentExamSessionId) 
    {
        bool redisAvailable = true;
        StudentExamSessionCacheDto? session = null;

        try
        {
            var db = _redisService.GetDatabase();
            string redisHashKey = $"student_exam_sessions:{studentCode}";
            string field = studentExamSessionId.ToString();

            var cachedValue = await db.HashGetAsync(redisHashKey, field);

            if (cachedValue.HasValue)
            {
                session = JsonSerializer.Deserialize<StudentExamSessionCacheDto>(cachedValue);
                // Kiểm tra xem phiên thi có hoàn thành không
                if (session != null && !session.IsCompleted)
                {
                    _logger.LogInformation("Đã lấy phiên thi {SessionId} chưa hoàn thành từ Redis cho sinh viên {StudentCode}", studentExamSessionId, studentCode);
                    return (true, session);
                }
                else if (session != null && session.IsCompleted)
                {
                    _logger.LogWarning("Phiên thi {SessionId} đã hoàn thành cho sinh viên {StudentCode}", studentExamSessionId, studentCode);
                    return (true, null);
                }
            }
           
            _logger.LogWarning("Không tìm thấy phiên thi {SessionId} trong Redis cho sinh viên {StudentCode}", studentExamSessionId, studentCode);
        }
        catch (Exception ex)
        {
            redisAvailable = false;
            _logger.LogWarning(ex, "Redis không khả dụng khi lấy phiên thi {SessionId} cho sinh viên {StudentCode}", studentExamSessionId, studentCode);
        }
        
        // Nếu Redis không có hoặc lỗi thì truy vấn DB
        try
        {
            var sessionEntity = await _studentExamSessionRepository.GetQueryable()
                .Where(x => x.StudentCode == studentCode && x.StudentExamSessionId == studentExamSessionId && x.IsCompleted == false)
                .Include(x => x.ExamSessionSubject)
                .ThenInclude(x => x.Subject)
                .Include(x => x.ExamRoom)
                .FirstOrDefaultAsync();

            if (sessionEntity != null)
            {
                session = await ConvertToCacheDtoAsync(sessionEntity);
                _logger.LogInformation("Đã lấy phiên thi {SessionId} chưa hoàn thành từ DB cho sinh viên {StudentCode}", studentExamSessionId, studentCode);

                if (redisAvailable)
                {
                    try
                    {
                        var db = _redisService.GetDatabase();
                        string redisHashKey = $"student_exam_sessions:{studentCode}";
                        string field = studentExamSessionId.ToString();
                        var sessionJson = JsonSerializer.Serialize(session);
                        await db.HashSetAsync(redisHashKey, field, sessionJson);
                        await db.KeyExpireAsync(redisHashKey, TimeSpan.FromHours(6));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Không thể ghi cache Redis cho phiên thi {SessionId} của sinh viên {StudentCode}", studentExamSessionId, studentCode);
                    }
                }

                return (redisAvailable, session);
            }

            _logger.LogWarning("Không tìm thấy phiên thi {SessionId} chưa hoàn thành cho sinh viên {StudentCode}", studentExamSessionId, studentCode);
            return (redisAvailable, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy phiên thi {SessionId} từ DB cho sinh viên {StudentCode}", studentExamSessionId, studentCode);
            return (redisAvailable, null);
        }
    }

   #endregion


   //Hoàn thành
   #region UpdateStudentExamSessionAsync
   public async Task UpdateStudentExamSessionAsync(string studentCode, StudentExamSessionCacheDto studentExamSessionDto)
   {
       try
       {
           string redisHashKey = $"student_exam_sessions:{studentCode}";
           string field = studentExamSessionDto.StudentExamSessionId.ToString();

           string sessionJson = JsonSerializer.Serialize(studentExamSessionDto);

           await _redisService.HashSetAsync(redisHashKey, field, sessionJson);

           _logger.LogInformation("Đã cập nhật phiên thi {SessionId} cho sinh viên {StudentCode} trong Redis", studentExamSessionDto.StudentExamSessionId, studentCode);
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Lỗi khi cập nhật phiên thi {SessionId} cho sinh viên {StudentCode} trong Redis", studentExamSessionDto.StudentExamSessionId, studentCode);
       }
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
            var db = _redisService.GetDatabase();
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
            var db = _redisService.GetDatabase();
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
    
 
     public async Task<(bool Success, string Message)> UpdateStudentAnswersInCacheAsync(string studentCode, int shuffledExamPaperId, string newAnswersString)
    {
        try
        {
            var db = _redisService.GetDatabase();
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