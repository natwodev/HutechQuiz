using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface IAcademicYearService
    {
        Task<IEnumerable<AcademicYearDto>> GetAllAsync();
        Task<AcademicYearDto?> GetByIdAsync(string id);
        Task<AcademicYearDto> AddAsync(AcademicYearCreateDto dto);
        Task<AcademicYearDto> UpdateAsync(int id, AcademicYearUpdateDto dto);
        Task<bool> DeleteAsync(string id);
    }
} 