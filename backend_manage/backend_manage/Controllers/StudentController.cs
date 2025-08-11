using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using OfficeOpenXml;
using System.IO;
using System.Linq;
using System.Security.Claims;
using backend_manage.core.Messages;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using backend_manage.shared.Interfaces;

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
        var result = await _studentService.LoginAsync(request.StudentCode1, request.StudentCode2);
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
    [Authorize(Policy = "StudentOnly")]
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
            students = result
        });
    }
    
    [HttpPost("extra-minutes")]
    public async Task<IActionResult> AddExtraMinutes([FromBody] AddExtraMinutesDto dto)
    {
        try
        {
            await _studentService.AddExtraMinutesAsync(dto.StudentCode, dto.StudentExamSessionId, dto.ExtraMinutes, dto.ReasonForExtra);
            return Ok(new { message = "Cập nhật thời gian làm bài thêm thành công." });
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
        var studentCode = User.FindFirst("studentCode")?.Value;
        if (string.IsNullOrEmpty(studentCode))
        {
            return Unauthorized(new { success = false, message = "Không tìm thấy thông tin sinh viên" });
        }

        if (request.Index < 0)
        {
            return BadRequest(new { success = false, message = "Index không hợp lệ" });
        }

        if (string.IsNullOrWhiteSpace(request.Answer))
        {
            return BadRequest(new { success = false, message = "Đáp án không được để trống" });
        }

        var (success, message, newAnswersString) = await _studentService.UpdateSingleAnswerAsync(
            studentCode,
            request.StudentExamSessionId,
            request.Index,
            request.SubIndex, // truyền thêm vào
            request.Answer
        );

        if (success)
        {
            _logger.LogInformation("✅ Sinh viên {StudentCode} đã cập nhật đáp án tại vị trí {Index}{SubIndex}: {Answer}",
                studentCode,
                request.Index,
                request.SubIndex.HasValue ? $" (câu con {request.SubIndex})" : "",
                request.Answer);

            return Ok(new
            {
                success = true,
                message,
                data = new
                {
                    newAnswersString,
                    index = request.Index,
                    subIndex = request.SubIndex,
                    answer = request.Answer
                }
            });
        }
        else
        {
            _logger.LogWarning("⚠️ Sinh viên {StudentCode} không thể cập nhật đáp án tại vị trí {Index}{SubIndex}: {Message}",
                studentCode,
                request.Index,
                request.SubIndex.HasValue ? $" (câu con {request.SubIndex})" : "",
                message);

            return BadRequest(new { success = false, message });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Lỗi khi cập nhật đáp án cho sinh viên {StudentCode} tại vị trí {Index}{SubIndex}",
            User.FindFirst("studentCode")?.Value,
            request.Index,
            request.SubIndex);

        return StatusCode(500, new { success = false, message = "Lỗi server khi cập nhật đáp án" });
    }
}

    [HttpPost("submit-exam")]
    public async Task<IActionResult> SubmitExam([FromBody] SubmitExamRequest request)
    {
        try
        {
            var studentCode = User.FindFirst("studentCode")?.Value;
            if (string.IsNullOrEmpty(studentCode))
            {
                return Unauthorized(new { success = false, message = "Không tìm thấy thông tin sinh viên" });
            }

            if (request.StudentExamSessionId <= 0)
            {
                return BadRequest(new { success = false, message = "ID phiên thi không hợp lệ" });
            }

            _logger.LogInformation("🔄 Sinh viên {StudentCode} yêu cầu nộp bài thi cho phiên {SessionId}", 
                studentCode, request.StudentExamSessionId);

            var (success, message, submissionData) = await _studentService.SubmitExamAsync(
                studentCode, 
                request.StudentExamSessionId
            );

            if (success)
            {
                _logger.LogInformation("✅ Sinh viên {StudentCode} đã nộp bài thi thành công. Điểm: {Score}", 
                    studentCode, submissionData?.Score);

                return Ok(new
                {
                    success = true,
                    message,
                    data = submissionData
                });
            }
            else
            {
                _logger.LogWarning("⚠️ Sinh viên {StudentCode} không thể nộp bài thi: {Message}", 
                    studentCode, message);

                return BadRequest(new { success = false, message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi nộp bài thi cho sinh viên {StudentCode}", 
                User.FindFirst("studentCode")?.Value);

            return StatusCode(500, new { success = false, message = "Lỗi server khi nộp bài thi" });
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

public class SubmitExamRequest
{
    public int StudentExamSessionId { get; set; }
}

public class LoginRequest
{
    public string StudentCode1 { get; set; }
    public string StudentCode2 { get; set; }
} 
