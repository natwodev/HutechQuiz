using backend_manage.Entities;
using backend_manage.DTOs;

namespace backend_manage.Services.Interfaces;

public interface IStudentService
{
    Task<StudentAuthResultDto> LoginAsync(string username, string password);
    Task<IEnumerable<Student>> GetAllAsync();
    Task<Student> AddAsync(StudentCreateDto dto);
    Task<Student> UpdateAsync(string id, Student student);
    Task<bool> DeleteAsync(string id);
    Task<IEnumerable<Student>> AddRangeAsync(IEnumerable<StudentCreateDto> dtos);
    Task<int> BulkImportStudentsAsync(List<StudentCreateDto> students);
    Task<StudentImportResultDto> ImportFromExcelAsync(IFormFile file, string examSessionSubjectCore, int examRoomId);
    Task<Student?> GetByStudentCodeAsync(string studentCode);
    Task<ShuffledExamPaperDto> StartExamAsync(string studentCode, int studentExamSessionId);
    Task<IEnumerable<StudentExamSessionDto>> GetStudentExamSessionsAsync(string studentCode);
    Task<IEnumerable<StudentExamRoomStatusDto>> GetStudentsByExamRoomAsync(int examRoomId, int examSessionSubjectId);
    Task<(bool Success, string Message)> AvtiveLoginAsync(string studentCode, bool isLogin);
    Task<bool> AddExtraMinutesAsync(string studentCode, int studentExamSessionId, int extraMinutes, string? reasonForExtra);
    Task<(bool Success, string Message)> SaveStudentAnswerAsync(string studentCode, int shuffledExamPaperId, int index, string answer);

    Task<(bool Success, string Message, double? Score)> SubmitExamAsync(string StudentCode, SubmitExamDto submitExamDto);
    Task<(bool Success, string Message)> SaveExamAsync(string StudentCode, SubmitExamDto submitExamDto);
    
    // Redis Cache Methods
    Task<(bool Success, string Message, int CachedCount)> PreloadStudentsToRedisAsync();
    Task<Student?> GetStudentFromRedisAsync(string studentCode);
    Task<bool> UpdateStudentInRedisAsync(Student student);
    Task<bool> RemoveStudentFromRedisAsync(string studentCode);
    Task<(bool Success, string Message, int CachedCount)> PreloadStudentExamSessionsToRedisAsync();
    Task<(bool Success, string Message, Dictionary<string, int> CachedCounts)> PreloadAllDataToRedisAsync();
    
    // Cache Management Methods
    Task<(bool Success, string Message, Dictionary<string, object> CacheInfo)> CheckCacheStatusAsync();
    Task<(bool Success, string Message, int DeletedCount)> ClearOldCacheAsync();
    Task<(bool Success, string Message, Dictionary<string, int> Results)> RefreshCacheAsync();
} 