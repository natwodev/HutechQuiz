using backend_manage.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend_manage.Services.Interfaces
{
    public interface IExamSessionSubjectService
    {
        Task<IEnumerable<ExamSessionSubjectDto>> GetAllAsync();
        Task<ExamSessionSubjectDto?> GetByIdAsync(string id);
        Task<ExamSessionSubjectDto> AddAsync(ExamSessionSubjectCreateDto dto);
        Task<ExamSessionSubjectDto> UpdateAsync(string id, ExamSessionSubjectUpdateDto dto);
        Task<bool> DeleteAsync(string id);
    }
} 