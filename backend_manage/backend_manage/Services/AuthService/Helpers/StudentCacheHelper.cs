using backend_manage.Entities;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace backend_manage.Services.AuthService.Helpers;

public class StudentCacheHelper
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<StudentCacheHelper> _logger;

    public StudentCacheHelper(IConnectionMultiplexer redis, ILogger<StudentCacheHelper> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<Student?> GetStudentFromRedisAsync(string studentCode)
    {
        try
        {
            var db = _redis.GetDatabase();
            string studentCacheKey = $"student:{studentCode}";
            
            // Thử lấy từ Redis cache trước
            var cachedStudent = await db.StringGetAsync(studentCacheKey);
            
            if (cachedStudent.HasValue)
            {
                try
                {
                    var cachedStudentObj = JsonSerializer.Deserialize<Student>(cachedStudent);
                    _logger.LogDebug("Đã lấy sinh viên {StudentCode} từ Redis cache", studentCode);
                    return cachedStudentObj;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi deserialize sinh viên {StudentCode} từ Redis cache, sẽ kiểm tra database", studentCode);
                    // Nếu lỗi deserialize, xóa cache và kiểm tra database
                    await db.KeyDeleteAsync(studentCacheKey);
                }
            }
            
            _logger.LogDebug("Không tìm thấy sinh viên {StudentCode} trong Redis cache", studentCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy sinh viên {StudentCode} từ Redis cache", studentCode);
            return null;
        }
    }

    public async Task<bool> UpdateStudentInRedisAsync(Student student)
    {
        try
        {
            var db = _redis.GetDatabase();
            string studentCacheKey = $"student:{student.StudentCode}";
            
            var studentJson = JsonSerializer.Serialize(student);
            await db.StringSetAsync(studentCacheKey, studentJson, TimeSpan.FromHours(6));
            
            _logger.LogDebug("Đã cập nhật sinh viên {StudentCode} trong Redis cache", student.StudentCode);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật sinh viên {StudentCode} trong Redis cache", student.StudentCode);
            return false;
        }
    }

    public async Task<bool> RemoveStudentFromRedisAsync(string studentCode)
    {
        try
        {
            var db = _redis.GetDatabase();
            string studentCacheKey = $"student:{studentCode}";
            
            var result = await db.KeyDeleteAsync(studentCacheKey);
            
            if (result)
            {
                _logger.LogDebug("Đã xóa sinh viên {StudentCode} khỏi Redis cache", studentCode);
            }
            else
            {
                _logger.LogDebug("Không tìm thấy sinh viên {StudentCode} trong Redis cache để xóa", studentCode);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa sinh viên {StudentCode} khỏi Redis cache", studentCode);
            return false;
        }
    }

    public async Task<(bool Success, string Message, int CachedCount)> PreloadStudentsToRedisAsync(IEnumerable<Student> students)
    {
        try
        {
            _logger.LogInformation("Bắt đầu tải sinh viên lên Redis cache");
            
            var db = _redis.GetDatabase();
            
            if (!students.Any())
            {
                _logger.LogWarning("Không có sinh viên nào để cache");
                return (false, "Không có sinh viên nào để cache", 0);
            }
            
            var batch = db.CreateBatch();
            var cacheTasks = new List<Task>();
            int cachedCount = 0;
            int updatedCount = 0;
            int skippedCount = 0;
            
            foreach (var student in students)
            {
                try
                {
                    // Tạo cache key cho từng sinh viên
                    string studentCacheKey = $"student:{student.StudentCode}";
                    
                    // Kiểm tra xem sinh viên đã tồn tại trong cache chưa
                    var existingStudent = await db.StringGetAsync(studentCacheKey);
                    
                    if (existingStudent.HasValue)
                    {
                        try
                        {
                            var cachedStudent = JsonSerializer.Deserialize<Student>(existingStudent);
                            
                            // So sánh version hoặc UpdatedAt để quyết định có cập nhật không
                            if (cachedStudent != null && 
                                (cachedStudent.Version < student.Version || 
                                 cachedStudent.UpdatedAt < student.UpdatedAt))
                            {
                                // Cập nhật nếu có thay đổi
                                var studentJson = JsonSerializer.Serialize(student);
                                var cacheTask = batch.StringSetAsync(studentCacheKey, studentJson, TimeSpan.FromHours(6));
                                cacheTasks.Add(cacheTask);
                                updatedCount++;
                                _logger.LogDebug("Cập nhật sinh viên {StudentCode} trong Redis cache", student.StudentCode);
                            }
                            else
                            {
                                // Bỏ qua nếu không có thay đổi
                                skippedCount++;
                                _logger.LogDebug("Bỏ qua sinh viên {StudentCode} - đã tồn tại và không có thay đổi", student.StudentCode);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Lỗi khi deserialize sinh viên {StudentCode} từ cache, sẽ cập nhật lại", student.StudentCode);
                            // Nếu lỗi deserialize, cập nhật lại
                            var studentJson = JsonSerializer.Serialize(student);
                            var cacheTask = batch.StringSetAsync(studentCacheKey, studentJson, TimeSpan.FromHours(6));
                            cacheTasks.Add(cacheTask);
                            updatedCount++;
                        }
                    }
                    else
                    {
                        // Thêm mới nếu chưa tồn tại
                        var studentJson = JsonSerializer.Serialize(student);
                        var cacheTask = batch.StringSetAsync(studentCacheKey, studentJson, TimeSpan.FromHours(6));
                        cacheTasks.Add(cacheTask);
                        cachedCount++;
                        _logger.LogDebug("Thêm mới sinh viên {StudentCode} vào Redis cache", student.StudentCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý cache cho sinh viên {StudentCode}", student.StudentCode);
                }
            }
            
            // Thực hiện batch cache
            if (cacheTasks.Any())
            {
                batch.Execute();
                await Task.WhenAll(cacheTasks);
            }
            
            var totalProcessed = cachedCount + updatedCount + skippedCount;
            var message = $"Đã xử lý {totalProcessed} sinh viên: Thêm mới {cachedCount}, Cập nhật {updatedCount}, Bỏ qua {skippedCount}";
            
            _logger.LogInformation("Hoàn thành tải sinh viên lên Redis cache. {Message}", message);
            
            return (true, message, totalProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải sinh viên lên Redis cache");
            return (false, $"Lỗi khi tải sinh viên lên Redis cache: {ex.Message}", 0);
        }
    }
} 