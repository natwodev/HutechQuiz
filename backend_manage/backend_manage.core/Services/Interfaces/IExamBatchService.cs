using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface IExamBatchService
    {
        Task<IEnumerable<ExamBatchDto>> GetAllAsync();
        Task<PagedResult<ExamBatchDto>> GetPagedAsync(int page, int pageSize);
        Task<ExamBatchDto?> GetByIdAsync(int id);
        Task<ExamBatchDto> AddAsync(ExamBatchCreateDto dto);
        Task<ExamBatchDto> UpdateAsync(int id, ExamBatchUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleIsActiveAsync(int id);
    }
} 