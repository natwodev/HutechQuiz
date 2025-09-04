using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface IDepartmentService
    {
        Task<IEnumerable<DepartmentDto>> GetAllAsync();
        Task<DepartmentDto?> GetByIdAsync(string id);
        Task<DepartmentDto> AddAsync(DepartmentCreateDto dto);
        Task<DepartmentDto> UpdateAsync(string id, DepartmentUpdateDto dto);
        Task<bool> DeleteAsync(string id);
    }
} 