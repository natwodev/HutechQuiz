using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.Http;

namespace backend_manage.core.Services.Interfaces
{
    public interface ILecturerService
    {
        Task<LecturerDto> AddLecturerAsync(LecturerCreateDto dto);
        Task<IEnumerable<LecturerDto>> GetAllLecturersAsync();
        Task<LecturerDto> GetByLecturerCodeAsync(string lecturerCode);
        Task<LecturerAuthResultDto> LoginAsync(string lecturerCode1, string lecturerCode2);
        Task<LecturerDto> GetProfileAsync(string lecturerCode);

        // Monitor actions
        Task<(bool Success, string Message)> ForceSubmitAsync(
            int studentExamSessionId,
            string studentCode);

        // Import from Excel
        Task<LecturerImportResultDto> ImportFromExcelAsync(IFormFile file);

        // Download Excel template
        Task<byte[]> DownloadExcelTemplateAsync();

        // ForceSubmitRoomAsync: not used
    }
} 