using backend_manage.DTOs;

namespace backend_manage.Services.Interfaces
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