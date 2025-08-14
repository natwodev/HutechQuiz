using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface IExamRoomLecturerAssignmentService
    {
        Task<IEnumerable<ExamRoomLecturerAssignmentDto>> GetAllAsync();
        Task<ExamRoomLecturerAssignmentDto?> GetByIdAsync(int id);
        Task<IEnumerable<ExamRoomLecturerAssignmentDto>> GetByLecturerIdAsync(int lecturerId);
        Task<IEnumerable<ExamRoomLecturerAssignmentDto>> GetByLecturerCodeAsync(string lecturerCode);
        Task<ExamRoomLecturerAssignmentDto> AddAsync(ExamRoomLecturerAssignmentCreateDto dto);
        Task<ExamRoomLecturerAssignmentDto?> UpdateAsync(int id, ExamRoomLecturerAssignmentCreateDto dto);
        Task<bool> DeleteAsync(int id);
    }
} 