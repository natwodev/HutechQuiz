using backend_manage.core.Entities;
using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces;

public interface IStudentActivityService
{
    /// <summary>
    /// Ghi nhận một hành động của sinh viên
    /// </summary>
    Task<StudentActivity> RecordActivityAsync(RecordActivityDto dto);

    /// <summary>
    /// Lấy danh sách hành động của sinh viên theo ExamSessionSubjectId
    /// </summary>
    Task<IEnumerable<StudentActivityDto>> GetActivitiesByExamSessionSubjectAsync(int examSessionSubjectId, DateTime? fromTime = null, DateTime? toTime = null);

    /// <summary>
    /// Lấy danh sách hành động của một sinh viên cụ thể
    /// </summary>
    Task<IEnumerable<StudentActivityDto>> GetActivitiesByStudentCodeAsync(string studentCode, int? examSessionSubjectId = null);

    /// <summary>
    /// Lấy thống kê hành động theo ExamSessionSubjectId
    /// </summary>
    Task<ActivityStatisticsDto> GetActivityStatisticsAsync(int examSessionSubjectId);
}

