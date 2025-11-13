using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface IExamRoomService
    {
        Task<IEnumerable<ExamRoomDto>> GetAllAsync();
        Task<ExamRoomDto?> GetByIdAsync(int id);
    }
}

