using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface IExamBatchService
    {
        Task<IEnumerable<ExamBatchDto>> GetAllAsync();
        Task<ExamBatchDto?> GetByIdAsync(string id);
        Task<ExamBatchDto> AddAsync(ExamBatchCreateDto dto);
        Task<ExamBatchDto> UpdateAsync(string id, ExamBatchUpdateDto dto);
        Task<bool> DeleteAsync(string id);
        Task<bool> ToggleIsActiveAsync(string id);
    }
} 