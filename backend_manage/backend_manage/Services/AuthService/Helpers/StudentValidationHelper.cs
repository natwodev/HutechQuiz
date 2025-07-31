using backend_manage.Entities;
using backend_manage.Repositories;
using backend_manage.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using backend_manage.Repositories.Interfaces;

namespace backend_manage.Services.AuthService.Helpers;

public class StudentValidationHelper
{
    private readonly ILogger<StudentValidationHelper> _logger;
    private readonly IRepository<StudentExamSession> _studentExamSessionRepository;
    private readonly IConnectionMultiplexer _redis;

    public StudentValidationHelper(
        ILogger<StudentValidationHelper> logger,
        IRepository<StudentExamSession> studentExamSessionRepository,
        IConnectionMultiplexer redis)
    {
        _logger = logger;
        _studentExamSessionRepository = studentExamSessionRepository;
        _redis = redis;
    }

    public async Task<(bool Success, string Message)> ValidateStudentExamSessionAsync(string studentCode, int shuffledExamPaperId)
    {
        var db = _redis.GetDatabase();
        
        // Tìm StudentExamSession từ cache với ShuffledExamPaperId cụ thể
        var sessionCacheKey = $"student_exam_session:{studentCode}:*";
        var keys = db.Multiplexer.GetServer(db.Multiplexer.GetEndPoints().First()).Keys(pattern: sessionCacheKey);
        
        foreach (var key in keys)
        {
            try
            {
                // Đọc dữ liệu dưới dạng string (vì được lưu bằng StringSetAsync)
                var sessionData = await db.StringGetAsync(key);
                if (sessionData.HasValue)
                {
                    var cachedStudentExamSession = JsonSerializer.Deserialize<StudentExamSession>(sessionData);
                    if (cachedStudentExamSession != null && cachedStudentExamSession.ShuffledExamPaperId == shuffledExamPaperId)
                    {
                        _logger.LogInformation($"[ValidateStudentExamSessionAsync] Truy vấn trạng thái từ cache StudentExamSession: {{Key}} = {{IsCompleted}}", key, cachedStudentExamSession.IsCompleted);
                        if (cachedStudentExamSession.IsCompleted)
                            return (false, "Bài thi đã được nộp trước đó");
                        return (true, "Validation thành công");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi đọc StudentExamSession từ Redis cache với key: {Key}", key);
                continue;
            }
        }

        _logger.LogInformation($"[ValidateStudentExamSessionAsync] Không có trạng thái trong cache, truy vấn DB");
        // Nếu không có trong cache, truy vấn DB
        var studentExamSession = await _studentExamSessionRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StudentCode == studentCode 
                && x.ShuffledExamPaperId == shuffledExamPaperId);

        if (studentExamSession == null)
            return (false, "Không tìm thấy phiên thi của sinh viên");

        if (studentExamSession.IsCompleted)
            return (false, "Bài thi đã được nộp trước đó");

        return (true, "Validation thành công");
    }

    public async Task<(bool Success, string Message)> ValidateStudentExamSessionOptimizedAsync(string studentCode, int shuffledExamPaperId, StudentExamSession? cachedSession)
    {
        if (cachedSession != null)
        {
            if (cachedSession.IsCompleted)
                return (false, "Bài thi đã được nộp trước đó");
            return (true, "Validation thành công");
        }
        
        // Fallback to database query
        var studentExamSession = await _studentExamSessionRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StudentCode == studentCode && x.ShuffledExamPaperId == shuffledExamPaperId);
        
        if (studentExamSession == null)
            return (false, "Không tìm thấy phiên thi của sinh viên");
        
        if (studentExamSession.IsCompleted)
            return (false, "Bài thi đã được nộp trước đó");
        
        return (true, "Validation thành công");
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
                            // Note: Cần inject IMapper nếu muốn sử dụng AutoMapper
                            // Tạm thời return null và để StudentService xử lý
                            return null;
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
} 