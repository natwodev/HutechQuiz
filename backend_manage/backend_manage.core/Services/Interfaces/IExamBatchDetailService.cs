using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface IExamBatchDetailService
    {
        Task<IEnumerable<ExamBatchDetailDto>> GetAllAsync();
        Task<ExamBatchDetailDto?> GetByIdAsync(int id);
        Task<ExamBatchDetailDto> AddAsync(ExamBatchDetailCreateDto dto);
        Task<ExamBatchDetailDto> UpdateAsync(int id, ExamBatchDetailUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
} 