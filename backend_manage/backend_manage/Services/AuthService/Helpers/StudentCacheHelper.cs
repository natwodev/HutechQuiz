using backend_manage.Entities;
using backend_manage.Repositories.Interfaces;
using backend_manage.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace backend_manage.Services.AuthService.Helpers;

public class StudentCacheHelper
{
    private readonly IRedisService _redisService;
    private readonly ILogger<StudentCacheHelper> _logger;
    private readonly IRepository<Student> _studentRepository;

    public StudentCacheHelper(
        IRedisService redisService,
        ILogger<StudentCacheHelper> logger,
        IRepository<Student> studentRepository)
    {
        _redisService = redisService;
        _logger = logger;
        _studentRepository = studentRepository;
    }

    #region GetStudentFromRedisAsync

    public async Task<Student?> GetStudentFromRedisAsync(string studentCode)
    {
        // B1: Lấy từ cache
        _logger.LogInformation("Bắt đầu tìm sinh viên với key {Key}", studentCode);
        var (redisAvailable, cachedStudent) = await GetStudentFromCache(studentCode);
        if (cachedStudent != null)
        {
            _logger.LogInformation("Tìm thấy sinh viên với {key} ở redis", studentCode);
            return cachedStudent;
        }

        // Redis có "null" → vẫn truy vấn DB, KHÔNG return null ở đây nữa
        // B2: Fallback DB
        if (!redisAvailable)
        {
            _logger.LogInformation("Tìm kiếm sinh viên ở db vì không kết nối được với redis: {key}", studentCode);
        }

        var dbStudent = await GetStudentFromDb(studentCode);

        // B3: Cache lại nếu Redis hoạt động
        if (redisAvailable)
            await CacheStudent(studentCode, dbStudent);

        return dbStudent;
    }

    private async Task<Student?> GetStudentFromDb(string studentCode)
    {
        try
        {
            var student = await _studentRepository
                .GetByConditionAsync(s => s.StudentCode == studentCode);
            if (student == null)
            {
                _logger.LogInformation("Không có sinh viên nào với mã: {key}", studentCode);
            }
            else
            {
                _logger.LogInformation("Đã tìm thấy sinh viên với mã: {key} từ DB", studentCode);
            }
            return student;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi truy vấn sinh viên: {StudentCode} từ DB", studentCode);
            return null;
        }
    }
    

private async Task CacheStudent(string studentCode, Student? student)
    {
        try
        {
            string key = $"student:{studentCode}";
            if (student == null)
            {
                _logger.LogInformation("Redis cache dữ liệu sinh viên null vì k có sinh viên {Key}", studentCode);
                await _redisService.StringSetAsync(key, "null", TimeSpan.FromMinutes(3));
            }
            else
            {
                _logger.LogInformation("Redis cache dữ liệu sinh viên thành công {Key}", studentCode);
                var json = JsonSerializer.Serialize(student);
                await _redisService.StringSetAsync(key, json, TimeSpan.FromMinutes(30));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể cache sinh viên với key {Key}", studentCode);
        }
    }
    private async Task<(bool redisAvailable, Student? student)> GetStudentFromCache(string studentCode)
    {
        try
        {
            string key = $"student:{studentCode}";
            var cachedValue = await _redisService.StringGetAsync(key);

            if (cachedValue == null)
            {
                _logger.LogInformation("Redis không có dữ liệu cho {Key}", studentCode);
                return (_redisService.IsConnected, null); 
            }

            if (cachedValue == "null")
            {
                _logger.LogInformation("Redis cache null cho {Key}", studentCode);
                return (_redisService.IsConnected, null); // Redis hoạt động, nhưng là null
            }

            var student = JsonSerializer.Deserialize<Student>(cachedValue);
            return (_redisService.IsConnected, student);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis lỗi khi lấy sinh viên từ key {Key}", studentCode);
            return (false, null); // Redis lỗi
        }
    }


    #endregion
    
    public async Task<(bool Success, string Message, int CachedCount)> PreloadStudentsToRedisAsync(IEnumerable<Student> students)
    {
        try
        {
            _logger.LogInformation("Bắt đầu tải sinh viên lên Redis cache");
            
            if (!students.Any())
            {
                _logger.LogWarning("Không có sinh viên nào để cache");
                return (false, "Không có sinh viên nào để cache", 0);
            }
            
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
                    var existingStudent = await _redisService.StringGetAsync(studentCacheKey);
                    
                    if (existingStudent != null)
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
                                await _redisService.StringSetAsync(studentCacheKey, studentJson, TimeSpan.FromHours(6));
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
                            await _redisService.StringSetAsync(studentCacheKey, studentJson, TimeSpan.FromHours(6));
                            updatedCount++;
                        }
                    }
                    else
                    {
                        // Thêm mới nếu chưa tồn tại
                        var studentJson = JsonSerializer.Serialize(student);
                        await _redisService.StringSetAsync(studentCacheKey, studentJson, TimeSpan.FromHours(6));
                        cachedCount++;
                        _logger.LogDebug("Thêm mới sinh viên {StudentCode} vào Redis cache", student.StudentCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý cache cho sinh viên {StudentCode}", student.StudentCode);
                }
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