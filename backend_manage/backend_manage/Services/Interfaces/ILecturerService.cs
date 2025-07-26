using System.Collections.Generic;
using System.Threading.Tasks;
using backend_manage.DTOs;

namespace backend_manage.Services.Interfaces
{
    public interface ILecturerService
    {
        Task<LecturerDto> AddLecturerAsync(LecturerCreateDto dto);
        Task<IEnumerable<LecturerDto>> GetAllLecturersAsync();
        Task<LecturerDto> GetByLecturerCodeAsync(string lecturerCode);
    }
} 