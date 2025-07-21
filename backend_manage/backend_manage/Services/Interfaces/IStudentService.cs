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
    Task<int> ImportFromExcelAsync(IFormFile file, string examSessionSubjectCore, int examRoomId);
    Task<Student?> GetByStudentCodeAsync(string studentCode);
    Task<ShuffledExamPaperDto> StartExamAsync(string studentCode, int examSessionSubjectId);
    Task<IEnumerable<StudentExamSessionDto>> GetStudentExamSessionsAsync(string studentCode);
} 