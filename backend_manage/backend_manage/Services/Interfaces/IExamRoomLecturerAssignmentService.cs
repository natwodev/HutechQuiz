using backend_manage.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend_manage.Services.Interfaces
{
    public interface IExamRoomLecturerAssignmentService
    {
        Task<IEnumerable<ExamRoomLecturerAssignmentDto>> GetAllAsync();
        Task<ExamRoomLecturerAssignmentDto?> GetByIdAsync(int id);
        Task<IEnumerable<ExamRoomLecturerAssignmentDto>> GetByLecturerIdAsync(string applicationUserId);
    }
} 