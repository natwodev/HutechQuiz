using backend_manage.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend_manage.Services.Interfaces
{
    public interface IAcademicYearService
    {
        Task<IEnumerable<AcademicYearDto>> GetAllAsync();
        Task<AcademicYearDto?> GetByIdAsync(string id);
        Task<AcademicYearDto> AddAsync(AcademicYearCreateDto dto);
        Task<AcademicYearDto> UpdateAsync(string id, AcademicYearUpdateDto dto);
        Task<bool> DeleteAsync(string id);
    }
} 