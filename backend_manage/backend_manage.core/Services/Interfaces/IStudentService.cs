using backend_manage.core.Entities;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.Http;

namespace backend_manage.core.Services.Interfaces;

public interface IStudentService
{
    Task<StudentAuthResultDto> LoginAsync(string studentCode1, string studentCode2);

    Task<StudentAuthResultDto> LoginMobileAsync(string studentCode1, string studentCode2);
    Task<IEnumerable<Student>> GetAllAsync();
    Task<Student> AddAsync(StudentCreateDto dto);
    Task<Student> UpdateAsync(string id, Student student);
    Task<bool> DeleteAsync(string id);
    Task<StudentImportResultDto> ImportFromExcelAsync(IFormFile file, string examSessionSubjectCore);
    Task<Student?> GetByStudentCodeAsync(string studentCode);

    Task<StudentImportResultDto> ImportFromExcelAsyncs(IFormFile file, string examSessionSubjectCore);
// Trong IStudentService
    Task<(StudentExamSessionCacheDto studentExamSessionCacheDto, ShuffledExamPaperDto? shuffledExamPaperDto, OriginalExamPaperDto? originalExamPaperDto)>
        StartExamAsync(string studentCode,
            int studentExamSessionId);
    Task<(StudentExamSessionCacheDto studentExamSessionCacheDto, OriginalExamPaperDto? originalExamPaperDto)>
        StartExamWithOriginalPaperAsync(string studentCode, int studentExamSessionId);
    Task<IEnumerable<StudentExamSessionDto>> GetStudentExamSessionsAsync(string studentCode);
    Task<(IEnumerable<StudentExamRoomStatusDto> Students, SubjectExamRoomStatusDto SubjectInfo)> GetStudentsByExamSessionSubjectAsync(int? examSessionSubjectId);
    Task<(bool Success, string Message)> AvtiveLoginAsync(string studentCode, bool isLogin);
    Task AddExtraMinutesAsync(string studentCode, int studentExamSessionId, int extraMinutes, string? reasonForExtra);

    Task<(bool Success, string Message, string? NewAnswersString)> UpdateSingleAnswerAsync(string studentCode, int studentExamSessionId, int key, object value);
    
    Task<StudentImportResultDto> ImportFromExcelStreamAsync(Stream stream, string examSessionSubjectCore,
        string userId);
    
    // Tạo StudentExamSession mới từ OriginalExamPaperId và studentCode, trả về phiên thi + đề gốc
    Task<(StudentExamSessionCacheDto studentExamSessionCacheDto, OriginalExamPaperDto? originalExamPaperDto)>
        CreateSessionWithOriginalPaperAsync(string studentCode, int originalExamPaperId);
    
    // Method để nộp bài thi
    Task<(bool Success, string Message)> SubmitExamAsync(string studentCode, int studentExamSessionId);
    
    // Method để lấy kết quả nộp bài
    Task<ExamSubmissionDto?> GetSubmissionResultAsync(string studentCode, int studentExamSessionId);
    
    // Method để lấy danh sách điểm sinh viên theo ExamSessionSubjectId
    Task<(IEnumerable<StudentGradeDto> Grades, string SubjectCode)> GetStudentGradesByExamSessionSubjectAsync(int examSessionSubjectId);
    
    // Method để export Excel bảng điểm sinh viên theo ExamSessionSubjectId
    Task<byte[]> ExportStudentGradesToExcelAsync(IEnumerable<StudentGradeDto> grades);
    
    Task<IEnumerable<StudentExamSessionHistoryDto>> GetStudentExamSessionsByStudentCodeAsync(string studentCode);
    Task<AllSubjectRankingResponseDto> GetAllCompletedSubjectRankingsAsync(string studentCode);
} 