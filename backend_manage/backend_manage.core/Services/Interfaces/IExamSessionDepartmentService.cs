using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface IExamSessionDepartmentService
    {
        Task<IEnumerable<ExamSessionDepartmentDto>> GetAllAsync();
        Task<ExamSessionDepartmentDto?> GetByIdAsync(string id);
        Task<ExamSessionDepartmentDto> AddAsync(ExamSessionDepartmentCreateDto dto);
        Task<ExamSessionDepartmentDto> UpdateAsync(string id, ExamSessionDepartmentUpdateDto dto);
        Task<bool> DeleteAsync(string id);
    }
} 