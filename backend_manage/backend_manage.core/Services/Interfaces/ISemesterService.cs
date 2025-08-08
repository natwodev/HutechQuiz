using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface ISemesterService
    {
        Task<IEnumerable<SemesterDto>> GetAllAsync();
        Task<SemesterDto?> GetByIdAsync(string id);
        Task<SemesterDto> AddAsync(SemesterCreateDto dto);
        Task<SemesterDto> UpdateAsync(string id, SemesterUpdateDto dto);
        Task<bool> DeleteAsync(string id);
    }
} 