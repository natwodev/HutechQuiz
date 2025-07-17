using backend_manage.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend_manage.Services.Interfaces
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