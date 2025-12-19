using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static backend_manage.core.Hubs.DateTimeHelper;

namespace backend_manage.core.Services.AuthService;

public class StudentActivityService : IStudentActivityService
{
    private readonly IRepository<StudentActivity> _activityRepository;
    private readonly IRepository<StudentExamSession> _studentExamSessionRepository;
    private readonly IRepository<Student> _studentRepository;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<StudentActivityService> _logger;

    public StudentActivityService(
        IRepository<StudentActivity> activityRepository,
        IRepository<StudentExamSession> studentExamSessionRepository,
        IRepository<Student> studentRepository,
        IHubContext<NotificationHub> hubContext,
        ILogger<StudentActivityService> logger)
    {
        _activityRepository = activityRepository;
        _studentExamSessionRepository = studentExamSessionRepository;
        _studentRepository = studentRepository;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<StudentActivity> RecordActivityAsync(RecordActivityDto dto)
    {
        // Kiểm tra StudentExamSession có tồn tại không
        var session = await _studentExamSessionRepository.GetByIdAsync(dto.StudentExamSessionId);
        if (session == null)
        {
            throw new Exception($"Không tìm thấy phiên thi với ID: {dto.StudentExamSessionId}");
        }

        var activity = new StudentActivity
        {
            StudentExamSessionId = dto.StudentExamSessionId,
            StudentCode = dto.StudentCode,
            ActivityType = dto.ActivityType,
            Description = dto.Description,
            Metadata = dto.Metadata,
            ActivityTime = DateTimeHelper.GetVietnamTime(),
            CreatedBy = dto.StudentCode,
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        var savedActivity = await _activityRepository.AddAsync(activity);

        // Gửi thông báo real-time qua SignalR cho giám thị
        if (session.ExamSessionSubjectId.HasValue)
        {
            try
            {
                var student = await _studentRepository.GetByConditionAsync(s => s.StudentCode == dto.StudentCode);
                var studentName = student != null 
                    ? $"{student.FirstName} {student.LastName}".Trim() 
                    : dto.StudentCode;
                await _hubContext.Clients
                    .Group($"lecturer_subject_{session.ExamSessionSubjectId.Value}")
                    .SendAsync("StudentActivityDetected", new
                    {
                        activityId = savedActivity.StudentActivityId,
                        studentCode = dto.StudentCode,
                        studentName = studentName,
                        activityType = dto.ActivityType,
                        description = dto.Description,
                        activityTime = savedActivity.ActivityTime
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi SignalR notification cho hoạt động của sinh viên");
            }
        }

        return savedActivity;
    }

    public async Task<IEnumerable<StudentActivityDto>> GetActivitiesByExamSessionSubjectAsync(
        int examSessionSubjectId, 
        DateTime? fromTime = null, 
        DateTime? toTime = null)
    {
        var query = _activityRepository.GetQueryable()
            .Include(a => a.StudentExamSession)
            .ThenInclude(s => s.Student)
            .Where(a => a.StudentExamSession.ExamSessionSubjectId == examSessionSubjectId);

        if (fromTime.HasValue)
        {
            query = query.Where(a => a.ActivityTime >= fromTime.Value);
        }

        if (toTime.HasValue)
        {
            query = query.Where(a => a.ActivityTime <= toTime.Value);
        }

        var activities = await query
            .OrderByDescending(a => a.ActivityTime)
            .ToListAsync();

        return activities.Select(a => new StudentActivityDto
        {
            StudentActivityId = a.StudentActivityId,
            StudentExamSessionId = a.StudentExamSessionId,
            StudentCode = a.StudentCode,
            ActivityType = a.ActivityType,
            Description = a.Description,
            Metadata = a.Metadata,
            ActivityTime = a.ActivityTime,
            StudentName = a.StudentExamSession.Student != null 
                ? $"{a.StudentExamSession.Student.FirstName} {a.StudentExamSession.Student.LastName}".Trim() 
                : a.StudentCode
        });
    }

    public async Task<IEnumerable<StudentActivityDto>> GetActivitiesByStudentCodeAsync(
        string studentCode, 
        int? examSessionSubjectId = null)
    {
        var query = _activityRepository.GetQueryable()
            .Include(a => a.StudentExamSession)
            .ThenInclude(s => s.Student)
            .Where(a => a.StudentCode == studentCode);

        if (examSessionSubjectId.HasValue)
        {
            query = query.Where(a => a.StudentExamSession.ExamSessionSubjectId == examSessionSubjectId.Value);
        }

        var activities = await query
            .OrderByDescending(a => a.ActivityTime)
            .ToListAsync();

        return activities.Select(a => new StudentActivityDto
        {
            StudentActivityId = a.StudentActivityId,
            StudentExamSessionId = a.StudentExamSessionId,
            StudentCode = a.StudentCode,
            ActivityType = a.ActivityType,
            Description = a.Description,
            Metadata = a.Metadata,
            ActivityTime = a.ActivityTime,
            StudentName = a.StudentExamSession.Student != null 
                ? $"{a.StudentExamSession.Student.FirstName} {a.StudentExamSession.Student.LastName}".Trim() 
                : a.StudentCode
        });
    }


    public async Task<ActivityStatisticsDto> GetActivityStatisticsAsync(int examSessionSubjectId)
    {
        var activities = await _activityRepository.GetQueryable()
            .Include(a => a.StudentExamSession)
            .Where(a => a.StudentExamSession.ExamSessionSubjectId == examSessionSubjectId)
            .ToListAsync();

        var stats = new ActivityStatisticsDto
        {
            TotalActivities = activities.Count
        };

        // Thống kê theo loại hành động
        stats.ActivitiesByType = activities
            .GroupBy(a => a.ActivityType)
            .ToDictionary(g => g.Key, g => g.Count());

        // Thống kê theo sinh viên
        stats.ActivitiesByStudent = activities
            .GroupBy(a => a.StudentCode)
            .ToDictionary(g => g.Key, g => g.Count());

        return stats;
    }
}

