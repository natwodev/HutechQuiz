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
    Task<int> ImportFromExcelAsync(Microsoft.AspNetCore.Http.IFormFile file);
    Task<Student?> GetByStudentCodeAsync(string studentCode);
} 