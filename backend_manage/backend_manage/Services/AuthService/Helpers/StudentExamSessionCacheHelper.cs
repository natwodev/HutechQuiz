using backend_manage.Entities;
using backend_manage.DTOs;
using backend_manage.Repositories;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using AutoMapper;
using System.Text.Json;

namespace backend_manage.Services.AuthService.Helpers;

public class StudentExamSessionCacheHelper
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<StudentExamSessionCacheHelper> _logger;
    private readonly IRepository<ExamSessionSubject> _examSessionSubjectRepository;
    private readonly IRepository<ExamRoom> _examRoomRepository;
    private readonly IMapper _mapper;

    public StudentExamSessionCacheHelper(
        IConnectionMultiplexer redis, 
        ILogger<StudentExamSessionCacheHelper> logger,
        IRepository<ExamSessionSubject> examSessionSubjectRepository,
        IRepository<ExamRoom> examRoomRepository,
        IMapper mapper)
    {
        _redis = redis;
        _logger = logger;
        _examSessionSubjectRepository = examSessionSubjectRepository;
        _examRoomRepository = examRoomRepository;
        _mapper = mapper;
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
                
                return result;
            }
            
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