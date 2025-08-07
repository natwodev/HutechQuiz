using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface ILecturerService
    {
        Task<LecturerDto> AddLecturerAsync(LecturerCreateDto dto);
        Task<IEnumerable<LecturerDto>> GetAllLecturersAsync();
        Task<LecturerDto> GetByLecturerCodeAsync(string lecturerCode);
    }
} 