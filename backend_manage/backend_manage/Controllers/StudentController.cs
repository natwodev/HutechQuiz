using backend_manage.DTOs;
using backend_manage.Entities;
using backend_manage.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using OfficeOpenXml;
using System.IO;
using System.Linq;
using System.Security.Claims;
using backend_manage.Hubs;

namespace backend_manage.Controllers;

[ApiController]
[Route("api/[controller]")]

public class StudentController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentController> _logger;

    public StudentController(
        IStudentService studentService,
        ILogger<StudentController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }


 [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _studentService.LoginAsync(request.Username, request.Password);
        return Ok(result);
    }

    [HttpPost("import-excel")]
    public async Task<IActionResult> ImportExcel([FromForm] IFormFile file, [FromForm] string examSessionSubjectCore, [FromForm] int examRoomId)
    {
        var result = await _studentService.ImportFromExcelAsync(file, examSessionSubjectCore, examRoomId);
        return Ok(new { studentsAdded = result.StudentsAdded, studentExamSessionsAdded = result.StudentExamSessionsAdded });
    }

    [HttpGet("by-code/{studentCode}")]
    public async Task<IActionResult> GetByStudentCode(string studentCode)
    {
        var student = await _studentService.GetByStudentCodeAsync(studentCode);
        if (student == null) return NotFound();
        return Ok(student);
    }
    
    
    // GET: api/students/profile
    [HttpGet("profile")]
    public async Task<ActionResult<StudentDto>> GetProfile()
    {
        try
        {
            var studentCode = User.FindFirst("studentCode")?.Value;
            if (string.IsNullOrEmpty(studentCode))
                return Unauthorized(new { message = "Token không hợp lệ!" });

            var student = await _studentService.GetByStudentCodeAsync(studentCode);
            return Ok(student);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("start-exam")]
    public async Task<IActionResult> StartExam([FromForm] int studentExamSessionId)
    {
        var studentCode = User.FindFirst("studentCode")?.Value;
        if (string.IsNullOrEmpty(studentCode))
            return Unauthorized(new { message = "Token không hợp lệ!" });
        var result = await _studentService.StartExamAsync(studentCode, studentExamSessionId);
        if (result == null) return BadRequest(new { message = "Không thể bắt đầu làm bài." });
        return Ok(result);
    }

    [HttpGet("exam-sessions")]
    public async Task<IActionResult> GetStudentExamSessions()
    {
        var studentCode = User.FindFirst("studentCode")?.Value;
        if (string.IsNullOrEmpty(studentCode))
            return Unauthorized(new { message = "Token không hợp lệ!" });
        var result = await _studentService.GetStudentExamSessionsAsync(studentCode);
        return Ok(result);
    }

    [HttpGet("by-exam-room")]
    public async Task<IActionResult> GetStudentsByExamRoom([FromQuery] int examRoomId, [FromQuery] int examSessionSubjectId)
    {
        var result = await _studentService.GetStudentsByExamRoomAsync(examRoomId, examSessionSubjectId);
        return Ok(new {
            students = result,
            signalrEndpoint = "/notificationHub",
            groupName = $"room_{examRoomId}"
        });
    }
    [HttpPost("extra-minutes")]
    public async Task<IActionResult> AddExtraMinutes([FromBody] AddExtraMinutesDto dto)
    {
        try
        {
            var success = await _studentService.AddExtraMinutesAsync(dto.StudentCode, dto.StudentExamSessionId, dto.ExtraMinutes, dto.ReasonForExtra);
            if (success)
                return Ok(new { message = "Cập nhật thời gian làm bài thêm thành công." });
            return BadRequest(new { message = "Không thể cập nhật." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("active-login")]
    public async Task<IActionResult> ActiveLogin([FromBody] ActiveLoginRequest request)
    {
        var (success, message) = await _studentService.AvtiveLoginAsync(request.StudentCode, request.IsLogin);

        if (!success)
            return NotFound(new { message });

        return Ok(new { message });
    }

     [HttpPost("save")]
    public async Task<IActionResult> SaveAnswer([FromBody] SaveAnswerDto dto)
    {
        try
        {
            var studentCode = User.FindFirst("studentCode")?.Value;
            if (string.IsNullOrEmpty(studentCode))
                return Unauthorized(new { message = "Token không hợp lệ!" });

            var result = await _studentService.SaveStudentAnswerAsync(
                studentCode,
                dto.ShuffledExamPaperId,
                dto.Index,
                dto.Answer
            );

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu đáp án của sinh viên");
            return StatusCode(500, new { message = "Có lỗi xảy ra khi lưu đáp án" });
        }
    }

    [HttpPost("submit-exam")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> SubmitExam([FromBody] SubmitExamDto submitExamDto)
    {
        try
        {
            // Lấy studentCode từ token
            var studentCode = User.FindFirst("studentCode")?.Value;
            if (string.IsNullOrEmpty(studentCode))
                return Unauthorized(new { message = "Token không hợp lệ!" });
            

            var (success, message, score) = await _studentService.SubmitExamAsync(studentCode,submitExamDto);

            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new { 
                message,
                score,
                submittedAt = DateTimeHelper.GetVietnamTime()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi nộp bài thi");
            return StatusCode(500, new { message = "Có lỗi xảy ra khi nộp bài" });
        }
    }

    [HttpPost("save-exam")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> SaveExam([FromBody] SubmitExamDto submitExamDto)
    {
        try
        {
            // Lấy studentCode từ token
            var studentCode = User.FindFirst("studentCode")?.Value;
            if (string.IsNullOrEmpty(studentCode))
                return Unauthorized(new { message = "Token không hợp lệ!" });
            

            var (success, message) = await _studentService.SaveExamAsync(studentCode, submitExamDto);

            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new { 
                message,
                savedAt = DateTimeHelper.GetVietnamTime()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu bài thi");
            return StatusCode(500, new { message = "Có lỗi xảy ra khi lưu bài thi" });
        }
    }

    #region Redis Cache Endpoints
    [HttpPost("preload-students-to-redis")]
    [Authorize(Roles = "Admin,ITManager")]
    public async Task<IActionResult> PreloadStudentsToRedis()
    {
        try
        {
            var (success, message, cachedCount) = await _studentService.PreloadStudentsToRedisAsync();
            
            if (success)
            {
                return Ok(new { success = true, message, cachedCount });
            }
            else
            {
                return BadRequest(new { success = false, message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải sinh viên lên Redis cache");
            return StatusCode(500, new { success = false, message = "Lỗi server khi tải sinh viên lên Redis cache" });
        }
    }

    [HttpPost("preload-student-exam-sessions-to-redis")]
    [Authorize(Roles = "Admin,ITManager")]
    public async Task<IActionResult> PreloadStudentExamSessionsToRedis()
    {
        try
        {
            var (success, message, cachedCount) = await _studentService.PreloadStudentExamSessionsToRedisAsync();
            
            if (success)
            {
                return Ok(new { success = true, message, cachedCount });
            }
            else
            {
                return BadRequest(new { success = false, message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải StudentExamSessions lên Redis cache");
            return StatusCode(500, new { success = false, message = "Lỗi server khi tải StudentExamSessions lên Redis cache" });
        }
    }

    [HttpPost("preload-all-data-to-redis")]
    [Authorize(Roles = "Admin,ITManager")]
    public async Task<IActionResult> PreloadAllDataToRedis()
    {
        try
        {
            var (success, message, cachedCounts) = await _studentService.PreloadAllDataToRedisAsync();
            
            if (success)
            {
                return Ok(new { success = true, message, cachedCounts });
            }
            else
            {
                return BadRequest(new { success = false, message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải tất cả dữ liệu lên Redis cache");
            return StatusCode(500, new { success = false, message = "Lỗi server khi tải dữ liệu lên Redis cache" });
        }
    }

    [HttpGet("get-student-from-redis/{studentCode}")]
    [Authorize(Roles = "Admin,ITManager,Lecturer")]
    public async Task<IActionResult> GetStudentFromRedis(string studentCode)
    {
        try
        {
            var student = await _studentService.GetStudentFromRedisAsync(studentCode);
            
            if (student != null)
            {
                return Ok(new { success = true, student });
            }
            else
            {
                return NotFound(new { success = false, message = $"Không tìm thấy sinh viên {studentCode} trong Redis cache" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy sinh viên {StudentCode} từ Redis cache", studentCode);
            return StatusCode(500, new { success = false, message = "Lỗi server khi lấy sinh viên từ Redis cache" });
        }
    }

    [HttpPut("update-student-in-redis")]
    [Authorize(Roles = "Admin,ITManager")]
    public async Task<IActionResult> UpdateStudentInRedis([FromBody] Student student)
    {
        try
        {
            var success = await _studentService.UpdateStudentInRedisAsync(student);
            
            if (success)
            {
                return Ok(new { success = true, message = $"Đã cập nhật sinh viên {student.StudentCode} trong Redis cache" });
            }
            else
            {
                return BadRequest(new { success = false, message = "Lỗi khi cập nhật sinh viên trong Redis cache" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật sinh viên trong Redis cache");
            return StatusCode(500, new { success = false, message = "Lỗi server khi cập nhật sinh viên trong Redis cache" });
        }
    }

    [HttpDelete("remove-student-from-redis/{studentCode}")]
    [Authorize(Roles = "Admin,ITManager")]
    public async Task<IActionResult> RemoveStudentFromRedis(string studentCode)
    {
        try
        {
            var success = await _studentService.RemoveStudentFromRedisAsync(studentCode);
            
            if (success)
            {
                return Ok(new { success = true, message = $"Đã xóa sinh viên {studentCode} khỏi Redis cache" });
            }
            else
            {
                return BadRequest(new { success = false, message = $"Không thể xóa sinh viên {studentCode} khỏi Redis cache" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa sinh viên {StudentCode} khỏi Redis cache", studentCode);
            return StatusCode(500, new { success = false, message = "Lỗi server khi xóa sinh viên khỏi Redis cache" });
        }
    }
    #endregion

    #region Cache Management Endpoints
    [HttpGet("check-cache-status")]
    [Authorize(Roles = "Admin,ITManager")]
    public async Task<IActionResult> CheckCacheStatus()
    {
        try
        {
            var (success, message, cacheInfo) = await _studentService.CheckCacheStatusAsync();
            
            if (success)
            {
                return Ok(new { success = true, message, cacheInfo });
            }
            else
            {
                return BadRequest(new { success = false, message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi kiểm tra trạng thái cache");
            return StatusCode(500, new { success = false, message = "Lỗi server khi kiểm tra trạng thái cache" });
        }
    }

    [HttpPost("clear-old-cache")]
    [Authorize(Roles = "Admin,ITManager")]
    public async Task<IActionResult> ClearOldCache()
    {
        try
        {
            var (success, message, deletedCount) = await _studentService.ClearOldCacheAsync();
            
            if (success)
            {
                return Ok(new { success = true, message, deletedCount });
            }
            else
            {
                return BadRequest(new { success = false, message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa cache cũ");
            return StatusCode(500, new { success = false, message = "Lỗi server khi xóa cache cũ" });
        }
    }

    [HttpPost("refresh-cache")]
    [Authorize(Roles = "Admin,ITManager")]
    public async Task<IActionResult> RefreshCache()
    {
        try
        {
            var (success, message, results) = await _studentService.RefreshCacheAsync();
            
            if (success)
            {
                return Ok(new { success = true, message, results });
            }
            else
            {
                return BadRequest(new { success = false, message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi refresh cache");
            return StatusCode(500, new { success = false, message = "Lỗi server khi refresh cache" });
        }
    }
    #endregion

}

public class ActiveLoginRequest
{
    public string StudentCode { get; set; }
    public bool IsLogin { get; set; }
}
public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
} 
