using backend_manage.core.Entities;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.Http;

namespace backend_manage.shared.Interfaces;

public interface IStudentService
{
    Task<StudentAuthResultDto> LoginAsync(string studentCode1, string studentCode2);
    Task<IEnumerable<Student>> GetAllAsync();
    Task<Student> AddAsync(StudentCreateDto dto);
    Task<Student> UpdateAsync(string id, Student student);
    Task<bool> DeleteAsync(string id);
    Task<StudentImportResultDto> ImportFromExcelAsync(IFormFile file, string examSessionSubjectCore, int examRoomId);
    Task<Student?> GetByStudentCodeAsync(string studentCode);

    Task<StudentImportResultDto> ImportFromExcelAsyncs(IFormFile file, string examSessionSubjectCore, int examRoomId);
// Trong IStudentService
    Task<(StudentExamSessionCacheDto studentExamSessionCacheDto, ShuffledExamPaperDto? shuffledExamPaperDto)>
        StartExamAsync(string studentCode,
            int studentExamSessionId);
    Task<IEnumerable<StudentExamSessionDto>> GetStudentExamSessionsAsync(string studentCode);
    Task<(IEnumerable<StudentExamRoomStatusDto> Students, SubjectExamRoomStatusDto SubjectInfo)> GetStudentsByExamRoomAsync(int examRoomId, int examSessionSubjectId);
    Task<(bool Success, string Message)> AvtiveLoginAsync(string studentCode, bool isLogin);
    Task AddExtraMinutesAsync(string studentCode, int studentExamSessionId, int extraMinutes, string? reasonForExtra);
    
    Task<(bool Success, string Message, string? NewAnswersString)> UpdateSingleAnswerAsync(string studentCode, int studentExamSessionId, int index,int? SubIndex, string answer);
    
    Task<StudentImportResultDto> ImportFromExcelStreamAsync(Stream stream, string examSessionSubjectCore,
        int examRoomId, string userId);
    
    // Method để nộp bài thi
    Task<(bool Success, string Message, ExamSubmissionDto? SubmissionData)> SubmitExamAsync(string studentCode, int studentExamSessionId);
    
} 