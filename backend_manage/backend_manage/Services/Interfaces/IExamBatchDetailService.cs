using backend_manage.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend_manage.Services.Interfaces
{
    public interface IExamBatchDetailService
    {
        Task<IEnumerable<ExamBatchDetailDto>> GetAllAsync();
        Task<ExamBatchDetailDto?> GetByIdAsync(string id);
        Task<ExamBatchDetailDto> AddAsync(ExamBatchDetailCreateDto dto);
        Task<ExamBatchDetailDto> UpdateAsync(string id, ExamBatchDetailUpdateDto dto);
        Task<bool> DeleteAsync(string id);
    }
} 