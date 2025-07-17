using backend_manage.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend_manage.Services.Interfaces
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