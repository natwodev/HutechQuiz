using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface IExamSessionSubjectService
    {
        Task<IEnumerable<ExamSessionSubjectDto>> GetAllAsync();
        Task<PagedResult<ExamSessionSubjectDto>> GetPagedAsync(int page, int pageSize);
        Task<ExamSessionSubjectDto?> GetByIdAsync(int id);
        Task<ExamSessionSubjectDto> AddAsync(ExamSessionSubjectCreateDto dto);
        Task<ExamSessionSubjectDto> UpdateAsync(int id, ExamSessionSubjectUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> UpdateOriginalExamPaperIdAsync(int examSessionSubjectId, int originalExamPaperId);
        Task<IEnumerable<ExamSessionSubjectRoomDto>> GetAllWithRoomsAsync();
        Task<bool> UpdateExamRoomIdAsync(int examSessionSubjectId, int? examRoomId);
        Task UpdateIsActiveAsync(int examSessionSubjectId, bool isActive);
        Task<IEnumerable<ExamSessionSubjectDto>> GetByExamRoomIdAsync(int examRoomId);
        
        // Các phương thức mới cho việc phân công giảng viên
        Task AssignLecturerAsync(AssignLecturerDto dto);
        Task UnassignLecturerAsync(UnassignLecturerDto dto);
        Task<IEnumerable<ExamSessionSubjectDto>> GetByLecturerIdAsync(int lecturerId);
        
        // Phương thức mới trả về SubjectExamRoomStatus và danh sách sinh viên
        Task<(SubjectExamRoomStatusDto SubjectExamRoomStatus, IEnumerable<StudentExamRoomStatusDto> Students)> GetExamSessionSubjectWithStudentsAsync(int examSessionSubjectId);
        
        // Phương thức mới trả về danh sách SubjectExamRoomStatusDto theo LecturerId
        Task<IEnumerable<SubjectExamRoomStatusDto>> GetSubjectExamRoomStatusByLecturerIdAsync(int lecturerId);

        // Kiểm tra môn thi trong ca thi đã mở hay chưa
        Task<bool> IsOpenAsync(int examSessionSubjectId);
    }
} 