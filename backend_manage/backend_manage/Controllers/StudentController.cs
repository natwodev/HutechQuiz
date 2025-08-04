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
using backend_manage.Messages;

namespace backend_manage.Controllers;

[ApiController]
[Route("api/[controller]")]

public class StudentController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly ILogger<StudentController> _logger;
    private readonly IRedisService _redisService;

    public StudentController(
        IStudentService studentService,
        ILogger<StudentController> logger,
        IRedisService redisService)
    {
        _studentService = studentService;
        _logger = logger;
        _redisService = redisService;
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
        var result = await _studentService.ImportFromExcelAsyncs(file, examSessionSubjectCore, examRoomId);
        return Ok(new { 
            studentsAdded = result.StudentsAdded, 
            studentExamSessionsAdded = result.StudentExamSessionsAdded,
            jobId = result.JobId,
            message = "Import đã được gửi vào queue. Sử dụng jobId để theo dõi tiến trình."
        });
    }

    [HttpGet("import-progress/{jobId}")]
    public async Task<IActionResult> GetImportProgress(string jobId)
    {
        try
        {
            // Kiểm tra xem Redis có khả dụng không
            if (!_redisService.IsConnected)
            {
                _logger.LogWarning("Redis không khả dụng, không thể lấy progress cho job {JobId}", jobId);
                return StatusCode(503, new { message = "Hệ thống cache không khả dụng, vui lòng thử lại sau" });
            }

            var progressData = await _redisService.StringGetAsync($"import_progress:{jobId}");
            
            if (string.IsNullOrEmpty(progressData))
            {
                return NotFound(new { message = "Không tìm thấy job import với ID này" });
            }
            
            var progress = System.Text.Json.JsonSerializer.Deserialize<StudentImportProgressMessage>(progressData);
            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy progress cho job {JobId}", jobId);
            return StatusCode(500, new { message = "Lỗi server khi lấy progress" });
        }
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
        var (result,pp) = await _studentService.StartExamAsync(studentCode, studentExamSessionId);
        if (result == null) return BadRequest(new { message = "Không thể bắt đầu làm bài vì k có phiên thi." });
        return Ok(new { studentSession = result, examPaper = pp });
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

    [HttpPost("save-answer")]
    public async Task<IActionResult> UpdateAnswer([FromBody] SaveAnswerDto request)
    {
        try
        {
            // Lấy studentCode từ JWT token
            var studentCode = User.FindFirst("studentCode")?.Value;
            if (string.IsNullOrEmpty(studentCode))
            {
                return Unauthorized(new { success = false, message = "Không tìm thấy thông tin sinh viên" });
            }

            // Validate input
            if (request.Index < 0)
            {
                return BadRequest(new { success = false, message = "Index không hợp lệ" });
            }

            if (string.IsNullOrEmpty(request.Answer))
            {
                return BadRequest(new { success = false, message = "Đáp án không được để trống" });
            }

            // Cập nhật đáp án
            var (success, message, newAnswersString) = await _studentService.UpdateSingleAnswerAsync(
                studentCode, 
                request.StudentExamSessionId, 
                request.Index, 
                request.Answer
            );

            if (success)
            {
                _logger.LogInformation("✅ Sinh viên {StudentCode} đã cập nhật đáp án tại vị trí {Index}: {Answer}", 
                    studentCode, request.Index, request.Answer);
                
                return Ok(new { 
                    success = true, 
                    message = message,
                    data = new { 
                        newAnswersString = newAnswersString,
                        index = request.Index,
                        answer = request.Answer
                    }
                });
            }
            else
            {
                _logger.LogWarning("⚠️ Sinh viên {StudentCode} không thể cập nhật đáp án tại vị trí {Index}: {Message}", 
                    studentCode, request.Index, message);
                
                return BadRequest(new { success = false, message = message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi cập nhật đáp án cho sinh viên {StudentCode} tại vị trí {Index}", 
                User.FindFirst("studentCode")?.Value, request.Index);
            
            return StatusCode(500, new { success = false, message = "Lỗi server khi cập nhật đáp án" });
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
