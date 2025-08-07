using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface IExamSessionSubjectService
    {
        Task<IEnumerable<ExamSessionSubjectDto>> GetAllAsync();
        Task<ExamSessionSubjectDto?> GetByIdAsync(string id);
        Task<ExamSessionSubjectDto> AddAsync(ExamSessionSubjectCreateDto dto);
        Task<ExamSessionSubjectDto> UpdateAsync(string id, ExamSessionSubjectUpdateDto dto);
        Task<bool> DeleteAsync(string id);
        Task<bool> UpdateOriginalExamPaperIdAsync(int examSessionSubjectId, int originalExamPaperId);
        Task<IEnumerable<ExamSessionSubjectRoomDto>> GetAllWithRoomsAsync();
    }
} 