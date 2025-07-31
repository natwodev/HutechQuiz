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

    /// <summary>
    /// Cập nhật StudentAnswersString trong cache thay vì tạo key riêng
    /// </summary>
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
            
            _logger.LogWarning("Không tìm thấy session với ShuffledExamPaperId {ShuffledExamPaperId} cho sinh viên {StudentCode}", 
                shuffledExamPaperId, studentCode);
            return (false, "Không tìm thấy phiên thi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật StudentAnswersString trong cache cho sinh viên {StudentCode}", studentCode);
            return (false, "Lỗi khi cập nhật đáp án");
        }
    }

    /// <summary>
    /// Lấy StudentAnswersString từ cache
    /// </summary>
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
            
            _logger.LogDebug("Không tìm thấy StudentAnswersString trong cache cho sinh viên {StudentCode} với ShuffledExamPaperId {ShuffledExamPaperId}", 
                studentCode, shuffledExamPaperId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy StudentAnswersString từ cache cho sinh viên {StudentCode}", studentCode);
            return null;
        }
    }

    public async Task<IEnumerable<StudentExamSessionDto>?> GetStudentExamSessionsFromRedisCacheAsync(string studentCode)
    {
        try
        {
            var db = _redis.GetDatabase();
            string sessionCacheKey = $"student_exam_session:{studentCode}:*";
            
            var keys = db.Multiplexer.GetServer(db.Multiplexer.GetEndPoints().First()).Keys(pattern: sessionCacheKey);
            var cachedSessions = new List<StudentExamSessionCacheDto>();
            
            foreach (var key in keys)
            {
                try
                {
                    var sessionData = await db.StringGetAsync(key);
                    if (sessionData.HasValue)
                    {
                        var cachedSession = JsonSerializer.Deserialize<StudentExamSessionCacheDto>(sessionData);
                        if (cachedSession != null && !cachedSession.IsCompleted)
                        {
                            cachedSessions.Add(cachedSession);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi deserialize session từ Redis cache với key: {Key}", key);
                }
            }
            
            if (cachedSessions.Any())
            {
                // Convert CacheDto sang StudentExamSessionDto
                var result = new List<StudentExamSessionDto>();
                foreach (var cachedSession in cachedSessions)
                {
                    var sessionDto = new StudentExamSessionDto
                    {
                        StudentExamSessionId = cachedSession.StudentExamSessionId,
                        ExamSessionSubjectId = cachedSession.ExamSessionSubjectId,
                        SubjectName = cachedSession.SubjectName,
                        RoomName = cachedSession.RoomName,
                        Duration = cachedSession.Duration,
                        ExtraMinutes = cachedSession.ExtraMinutes,
                        StartTime = cachedSession.StartTime ?? DateTime.MinValue,
                        EndTime = cachedSession.EndTime ?? DateTime.MinValue
                    };
                    result.Add(sessionDto);
                }
                
                _logger.LogDebug("Đã lấy {Count} phiên thi từ Redis cache cho sinh viên {StudentCode}", 
                    result.Count, studentCode);
                return result;
            }
            
            // Nếu không có trong Redis cache, lấy từ database
            _logger.LogDebug("Không tìm thấy phiên thi trong Redis cache cho sinh viên {StudentCode}, kiểm tra database", studentCode);
            
            var student = await _studentRepository.GetQueryable().FirstOrDefaultAsync(x => x.StudentCode == studentCode);
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
            
            if (sessions.Any())
            {
                // Cache lại vào Redis
                await CacheStudentExamSessionsForStudentAsync(studentCode, sessions);
                
                // Convert sang DTO
                var result = sessions.Select(x => _mapper.Map<StudentExamSessionDto>(x)).ToList();
                
                _logger.LogDebug("Đã lấy {Count} phiên thi từ database và cache lại cho sinh viên {StudentCode}", 
                    result.Count, studentCode);
                return result;
            }
            
            _logger.LogDebug("Không tìm thấy phiên thi nào cho sinh viên {StudentCode}", studentCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy phiên thi từ Redis cache cho sinh viên {StudentCode}", studentCode);
            return null;
        }
    }

    public async Task CacheStudentExamSessionsForStudentAsync(string studentCode, List<StudentExamSession> sessions)
    {
        try
        {
            var db = _redis.GetDatabase();
            int cachedCount = 0;
            
            foreach (var session in sessions)
            {
                try
                {
                    string sessionCacheKey = $"student_exam_session:{studentCode}:{session.StudentExamSessionId}";
                    var cacheDto = await ConvertToCacheDtoAsync(session);
                    var sessionJson = JsonSerializer.Serialize(cacheDto);
                    await db.StringSetAsync(sessionCacheKey, sessionJson, TimeSpan.FromHours(6));
                    cachedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi cache session {SessionId} cho sinh viên {StudentCode}", 
                        session.StudentExamSessionId, studentCode);
                }
            }
            
            _logger.LogDebug("Đã cache {CachedCount} phiên thi cho sinh viên {StudentCode}", cachedCount, studentCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cache phiên thi cho sinh viên {StudentCode}", studentCode);
        }
    }

    public async Task<(bool Success, string Message, int CachedCount)> PreloadStudentExamSessionsToRedisAsync(IEnumerable<StudentExamSession> studentExamSessions)
    {
        try
        {
            _logger.LogInformation("Bắt đầu tải StudentExamSessions lên Redis cache");
            
            var db = _redis.GetDatabase();
            
            if (!studentExamSessions.Any())
            {
                _logger.LogWarning("Không có StudentExamSession nào để cache");
                return (false, "Không có StudentExamSession nào để cache", 0);
            }
            
            var batch = db.CreateBatch();
            var cacheTasks = new List<Task>();
            int cachedCount = 0;
            int updatedCount = 0;
            int skippedCount = 0;
            
            foreach (var session in studentExamSessions)
            {
                try
                {
                    // Tạo cache key cho từng session
                    string sessionCacheKey = $"student_exam_session:{session.StudentCode}:{session.StudentExamSessionId}";
                    
                    // Kiểm tra xem session đã tồn tại trong cache chưa
                    var existingSession = await db.StringGetAsync(sessionCacheKey);
                    
                    if (existingSession.HasValue)
                    {
                        try
                        {
                            var cachedSession = JsonSerializer.Deserialize<StudentExamSession>(existingSession);
                            
                            // So sánh version hoặc UpdatedAt để quyết định có cập nhật không
                            if (cachedSession != null && 
                                (cachedSession.Version < session.Version || 
                                 cachedSession.UpdatedAt < session.UpdatedAt ||
                                 cachedSession.StudentAnswersString != session.StudentAnswersString ||
                                 cachedSession.IsCompleted != session.IsCompleted))
                            {
                                // Cập nhật nếu có thay đổi
                                var cacheDto = await ConvertToCacheDtoAsync(session);
                                var sessionJson = JsonSerializer.Serialize(cacheDto);
                                var cacheTask = batch.StringSetAsync(sessionCacheKey, sessionJson, TimeSpan.FromHours(6));
                                cacheTasks.Add(cacheTask);
                                updatedCount++;
                                _logger.LogDebug("Cập nhật StudentExamSession {StudentCode}:{StudentExamSessionId} trong Redis cache", 
                                    session.StudentCode, session.StudentExamSessionId);
                            }
                            else
                            {
                                // Bỏ qua nếu không có thay đổi
                                skippedCount++;
                                _logger.LogDebug("Bỏ qua StudentExamSession {StudentCode}:{StudentExamSessionId} - đã tồn tại và không có thay đổi", 
                                    session.StudentCode, session.StudentExamSessionId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Lỗi khi deserialize StudentExamSession {StudentCode}:{StudentExamSessionId} từ cache, sẽ cập nhật lại", 
                                session.StudentCode, session.StudentExamSessionId);
                            // Nếu lỗi deserialize, cập nhật lại
                            var cacheDto = await ConvertToCacheDtoAsync(session);
                            var sessionJson = JsonSerializer.Serialize(cacheDto);
                            var cacheTask = batch.StringSetAsync(sessionCacheKey, sessionJson, TimeSpan.FromHours(6));
                            cacheTasks.Add(cacheTask);
                            updatedCount++;
                        }
                    }
                    else
                    {
                        // Thêm mới nếu chưa tồn tại
                        var cacheDto = await ConvertToCacheDtoAsync(session);
                        var sessionJson = JsonSerializer.Serialize(cacheDto);
                        var cacheTask = batch.StringSetAsync(sessionCacheKey, sessionJson, TimeSpan.FromHours(6));
                        cacheTasks.Add(cacheTask);
                        cachedCount++;
                        _logger.LogDebug("Thêm mới StudentExamSession {StudentCode}:{StudentExamSessionId} vào Redis cache", 
                            session.StudentCode, session.StudentExamSessionId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý cache cho StudentExamSession {StudentCode}:{StudentExamSessionId}", 
                        session.StudentCode, session.StudentExamSessionId);
                }
            }
            
            // Thực hiện batch cache
            if (cacheTasks.Any())
            {
                batch.Execute();
                await Task.WhenAll(cacheTasks);
            }
            
            var totalProcessed = cachedCount + updatedCount + skippedCount;
            var message = $"Đã xử lý {totalProcessed} StudentExamSession: Thêm mới {cachedCount}, Cập nhật {updatedCount}, Bỏ qua {skippedCount}";
            
            _logger.LogInformation("Hoàn thành tải StudentExamSessions lên Redis cache. {Message}", message);
            
            return (true, message, totalProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải StudentExamSessions lên Redis cache");
            return (false, $"Lỗi khi tải StudentExamSessions lên Redis cache: {ex.Message}", 0);
        }
    }

    public async Task<StudentExamSession?> GetStudentExamSessionFromCacheAsync(IDatabase db, string sessionCacheKey, int shuffledExamPaperId)
    {
        try
        {
            var keys = db.Multiplexer.GetServer(db.Multiplexer.GetEndPoints().First()).Keys(pattern: sessionCacheKey);
            
            foreach (var key in keys)
            {
                var sessionData = await db.StringGetAsync(key);
                if (sessionData.HasValue)
                {
                    try
                    {
                        // Thử deserialize thành CacheDto trước
                        var cachedSessionDto = JsonSerializer.Deserialize<StudentExamSessionCacheDto>(sessionData);
                        if (cachedSessionDto?.ShuffledExamPaperId == shuffledExamPaperId)
                        {
                            // Sử dụng AutoMapper để convert CacheDto về Entity
                            var sessionEntity = _mapper.Map<StudentExamSession>(cachedSessionDto);
                            return sessionEntity;
                        }
                    }
                    catch
                    {
                        // Fallback: thử deserialize thành Entity cũ
                        var cachedSession = JsonSerializer.Deserialize<StudentExamSession>(sessionData);
                        if (cachedSession?.ShuffledExamPaperId == shuffledExamPaperId)
                        {
                            return cachedSession;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi đọc StudentExamSession từ Redis cache");
        }
        
        return null;
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
    

    private async Task<StudentExamSessionCacheDto> ConvertToCacheDtoAsync(StudentExamSession session)
    {
        // Sử dụng AutoMapper để map từ Entity sang CacheDto
        var cacheDto = _mapper.Map<StudentExamSessionCacheDto>(session);
        
        // Nếu navigation properties chưa được load, load từ database
        if (string.IsNullOrEmpty(cacheDto.SubjectName) || cacheDto.Duration == 0)
        {
            var examSessionSubject = await _examSessionSubjectRepository.GetQueryable()
                .Include(ess => ess.Subject)
                .FirstOrDefaultAsync(ess => ess.ExamSessionSubjectId == session.ExamSessionSubjectId);
            
            if (examSessionSubject?.Subject != null)
            {
                cacheDto.SubjectName = examSessionSubject.Subject.SubjectName;
                cacheDto.Duration = examSessionSubject.Duration;
            }
        }
        
        if (string.IsNullOrEmpty(cacheDto.RoomName) && session.ExamRoomId.HasValue)
        {
            var examRoom = await _examRoomRepository.GetQueryable()
                .FirstOrDefaultAsync(er => er.ExamRoomId == session.ExamRoomId);
            
            if (examRoom != null)
            {
                cacheDto.RoomName = examRoom.RoomName;
            }
        }
        
        return cacheDto;
    }
} 