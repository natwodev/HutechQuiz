using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface IExamRoomLecturerAssignmentService
    {
        Task<IEnumerable<ExamRoomLecturerAssignmentDto>> GetAllAsync();
        Task<ExamRoomLecturerAssignmentDto?> GetByIdAsync(int id);
        Task<IEnumerable<ExamRoomLecturerAssignmentDto>> GetByLecturerIdAsync(string applicationUserId);
    }
} 