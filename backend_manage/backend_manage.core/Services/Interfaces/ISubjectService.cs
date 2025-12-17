using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface ISubjectService
    {
        Task<IEnumerable<SubjectDto>> GetAllAsync();
        Task<SubjectDto?> GetByIdAsync(int id);
        Task<SubjectDto> AddAsync(SubjectCreateDto dto);
        Task<SubjectDto?> UpdateAsync(int id, SubjectUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}

