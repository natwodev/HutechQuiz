using backend_manage.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend_manage.Services.Interfaces
{
    public interface IExamSessionService
    {
        Task<IEnumerable<ExamSessionDto>> GetAllAsync();
        Task<ExamSessionDto?> GetByIdAsync(string id);
        Task<ExamSessionDto> AddAsync(ExamSessionCreateDto dto);
        Task<ExamSessionDto> UpdateAsync(string id, ExamSessionUpdateDto dto);
        Task<bool> DeleteAsync(string id);
    }
} 