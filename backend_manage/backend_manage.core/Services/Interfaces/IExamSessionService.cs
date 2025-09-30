using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface IExamSessionService
    {
        Task<IEnumerable<ExamSessionDto>> GetAllAsync();
        Task<ExamSessionDto?> GetByIdAsync(int id);
        Task<ExamSessionDto> AddAsync(ExamSessionCreateDto dto);
        Task<ExamSessionDto> UpdateAsync(int id, ExamSessionUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
} 