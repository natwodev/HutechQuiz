using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using Microsoft.Extensions.Logging;

namespace backend_manage.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentActivityController : ControllerBase
{
    private readonly IStudentActivityService _studentActivityService;
    private readonly ILogger<StudentActivityController> _logger;

    public StudentActivityController(
        IStudentActivityService studentActivityService,
        ILogger<StudentActivityController> logger)
    {
        _studentActivityService = studentActivityService;
        _logger = logger;
    }

    /// <summary>
    /// Ghi nhận một hành động của sinh viên (từ phía client sinh viên)
    /// </summary>
    [HttpPost("record")]
    public async Task<IActionResult> RecordActivity([FromBody] RecordActivityDto dto)
    {
        if (dto == null)
            return BadRequest(new { message = "Dữ liệu không hợp lệ" });

        try
        {
            var activity = await _studentActivityService.RecordActivityAsync(dto);
            _logger.LogInformation("Đã ghi nhận hành động: {ActivityType} từ sinh viên: {StudentCode}", 
                dto.ActivityType, dto.StudentCode);
            return Ok(new { message = "Đã ghi nhận hành động", activityId = activity.StudentActivityId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi ghi nhận hành động từ sinh viên: {StudentCode}", dto.StudentCode);
            return StatusCode(500, new { message = $"Lỗi khi ghi nhận hành động: {ex.Message}" });
        }
    }

    /// <summary>
    /// Lấy danh sách hành động theo ExamSessionSubjectId (cho giám thị)
    /// </summary>
    [HttpGet("by-exam-session/{examSessionSubjectId}")]
    [Authorize(Policy = "LecturerOrAdmin")]
    public async Task<IActionResult> GetActivitiesByExamSession(
        int examSessionSubjectId,
        [FromQuery] DateTime? fromTime = null,
        [FromQuery] DateTime? toTime = null)
    {
        try
        {
            var activities = await _studentActivityService.GetActivitiesByExamSessionSubjectAsync(
                examSessionSubjectId, fromTime, toTime);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách hành động cho ExamSessionSubjectId: {ExamSessionSubjectId}", 
                examSessionSubjectId);
            return StatusCode(500, new { message = $"Lỗi khi lấy danh sách hành động: {ex.Message}" });
        }
    }

    /// <summary>
    /// Lấy danh sách hành động của một sinh viên cụ thể
    /// </summary>
    [HttpGet("by-student/{studentCode}")]
    [Authorize(Policy = "LecturerOrAdmin")]
    public async Task<IActionResult> GetActivitiesByStudent(
        string studentCode,
        [FromQuery] int? examSessionSubjectId = null)
    {
        try
        {
            var activities = await _studentActivityService.GetActivitiesByStudentCodeAsync(
                studentCode, examSessionSubjectId);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách hành động cho sinh viên: {StudentCode}", studentCode);
            return StatusCode(500, new { message = $"Lỗi khi lấy danh sách hành động: {ex.Message}" });
        }
    }

    /// <summary>
    /// Lấy thống kê hành động theo ExamSessionSubjectId
    /// </summary>
    [HttpGet("statistics/{examSessionSubjectId}")]
    [Authorize(Policy = "LecturerOrAdmin")]
    public async Task<IActionResult> GetStatistics(int examSessionSubjectId)
    {
        try
        {
            var stats = await _studentActivityService.GetActivityStatisticsAsync(examSessionSubjectId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thống kê hành động cho ExamSessionSubjectId: {ExamSessionSubjectId}", 
                examSessionSubjectId);
            return StatusCode(500, new { message = $"Lỗi khi lấy thống kê: {ex.Message}" });
        }
    }

    /// <summary>
    /// Lấy violation count của student hiện tại (chỉ cho phép student xem của chính họ)
    /// </summary>
    [HttpGet("my-violation-count")]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> GetMyViolationCount([FromQuery] int examSessionSubjectId)
    {
        try
        {
            // Lấy studentCode từ claim
            var studentCode = User.FindFirst("studentCode")?.Value 
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(studentCode))
            {
                return Unauthorized(new { message = "Không tìm thấy thông tin sinh viên" });
            }

            // Lấy activities của student này
            var activities = await _studentActivityService.GetActivitiesByStudentCodeAsync(
                studentCode, examSessionSubjectId);

            // Định nghĩa các loại vi phạm nghiêm trọng (giống frontend)
            var violationTypes = new HashSet<string>
            {
                "TabSwitch", "FullscreenExit", "Copy", "Paste", 
                "RightClick", "DevTools", "Screenshot"
            };

            // Đếm số lượng vi phạm
            var violationCount = activities.Count(a => violationTypes.Contains(a.ActivityType));

            return Ok(new 
            { 
                studentCode = studentCode,
                violationCount = violationCount,
                maxViolations = 3,
                totalActivities = activities.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy violation count cho student");
            return StatusCode(500, new { message = $"Lỗi khi lấy violation count: {ex.Message}" });
        }
    }
}

