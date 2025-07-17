using backend_manage.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend_manage.Services.Interfaces
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