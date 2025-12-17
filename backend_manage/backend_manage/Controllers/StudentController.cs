using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using OfficeOpenXml;
using System.IO;
using System.Linq;
using System.Security.Claims;
using backend_manage.core.Entities;
using backend_manage.core.Messages;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;

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
        var result = await _studentService.LoginAsync(request.StudentCode1, request.StudentCode2);
        return Ok(result);
    }

    [HttpPost("login-mobile")]
    public async Task<IActionResult> LoginMoblie([FromBody] LoginRequest request)
    {
        var result = await _studentService.LoginMobileAsync(request.StudentCode1, request.StudentCode2);
        return Ok(result);
    }
    [HttpPost("import-excel")]
    public async Task<IActionResult> ImportExcel([FromForm] IFormFile file, [FromForm] string examSessionSubjectCore)
    {
        var result = await _studentService.ImportFromExcelAsyncs(file, examSessionSubjectCore);
        return Ok(new { 
            studentsAdded = result.StudentsAdded, 
            studentExamSessionsAdded = result.StudentExamSessionsAdded,
            message = "Import sinh viên thành công."
        });
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
        try
        {
            var studentCode = User.FindFirst("studentCode")?.Value;
            if (string.IsNullOrEmpty(studentCode))
                return Unauthorized(new { message = "Token không hợp lệ!" });
            
            var (result, pp, originalPaper) = await _studentService.StartExamAsync(studentCode, studentExamSessionId);
            if (result == null) 
                return BadRequest(new { message = "Không thể bắt đầu làm bài vì không có phiên thi." });
            
            return Ok(new { studentSession = result, examPaper = pp, originalExamPaper = originalPaper });
        }
        catch (InvalidOperationException ex)
        {
            // Lỗi thời gian hoặc logic nghiệp vụ
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi bắt đầu thi cho sinh viên");
            return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau." });
        }
    }

    [HttpPost("start-exam-original")]
    public async Task<IActionResult> StartExamOriginal([FromForm] int studentExamSessionId)
    {
        try
        {
            var studentCode = User.FindFirst("studentCode")?.Value;
            if (string.IsNullOrEmpty(studentCode))
                return Unauthorized(new { message = "Token không hợp lệ!" });

            var (result, originalPaper) = await _studentService.StartExamWithOriginalPaperAsync(studentCode, studentExamSessionId);
            if (result == null)
                return BadRequest(new { message = "Không thể bắt đầu làm bài vì không có phiên thi." });

            return Ok(new { studentSession = result, originalExamPaper = originalPaper });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi bắt đầu thi (đề gốc) cho sinh viên");
            return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau." });
        }
    }
    
    /// <summary>
    /// Tạo một StudentExamSession mới từ OriginalExamPaperId và trả về phiên thi + đề gốc.
    /// </summary>
    [HttpPost("create-session-original")]
    public async Task<IActionResult> CreateSessionWithOriginalPaper([FromForm] int originalExamPaperId)
    {
        try
        {
            var studentCode = User.FindFirst("studentCode")?.Value;
            if (string.IsNullOrEmpty(studentCode))
                return Unauthorized(new { message = "Token không hợp lệ!" });

            var (session, originalPaper) =
                await _studentService.CreateSessionWithOriginalPaperAsync(studentCode, originalExamPaperId);

            if (session == null)
                return BadRequest(new { message = "Không thể tạo phiên thi." });

            return Ok(new
            {
                studentSession = session,
                originalExamPaper = originalPaper
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo phiên thi (đề gốc) cho sinh viên");
            return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau." });
        }
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

    [HttpGet("by-exam-session-subject")]
    public async Task<IActionResult> GetStudentsByExamSessionSubject([FromQuery] int examSessionSubjectId)
    {
        var (students, subject) = await _studentService.GetStudentsByExamSessionSubjectAsync(examSessionSubjectId);
        return Ok(new {
            subject,
            students
           
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

        if (request.key < 0)
        {
            return BadRequest(new { success = false, message = "Index không hợp lệ" });
        }

        if (string.IsNullOrWhiteSpace(request.value.ToString()))
        {
            return BadRequest(new { success = false, message = "Đáp án không được để trống" });
        }

        var (success, message, newAnswersString) = await _studentService.UpdateSingleAnswerAsync(
            studentCode,
            request.StudentExamSessionId,
            request.key,
            request.value // truyền thêm vào
        );

        if (success)
        {
            _logger.LogInformation("✅ Sinh viên {StudentCode} đã cập nhật đáp án tại vị trí {Key}: {Value}",
                studentCode,
                request.key,
                request.value);

            return Ok(new
            {
                success = true,
                message,
                data = new
                {
                    newAnswersString,
                    key = request.key,
                    value = request.value,
                }
            });
        }
        else
        {
            _logger.LogWarning("⚠️ Sinh viên {StudentCode} không thể cập nhật đáp án tại vị trí {Key}: {Value}",
                studentCode,
                request.key,
                request.value);

            return BadRequest(new { success = false, message });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Lỗi khi cập nhật đáp án cho sinh viên {StudentCode} tại vị trí {Key}: {Value}",
            User.FindFirst("studentCode")?.Value,
            request.key,
            request.value);

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
            return Unauthorized(new { message = "Không tìm thấy thông tin sinh viên" });
        }

        if (request.StudentExamSessionId <= 0)
        {
            return BadRequest(new { message = "ID phiên thi không hợp lệ" });
        }

        _logger.LogInformation("🔄 Sinh viên {StudentCode} yêu cầu nộp bài thi cho phiên {SessionId}", 
            studentCode, request.StudentExamSessionId);

        var (success, message) = await _studentService.SubmitExamAsync(studentCode, request.StudentExamSessionId);

        if (success)
        {
            _logger.LogInformation("✅ Sinh viên {StudentCode} đã nộp bài thi thành công", studentCode);

            return Ok(new
            {
                message = "Nộp bài thi thành công"
            });
        }
        else
        {
            _logger.LogWarning("⚠️ Sinh viên {StudentCode} không thể nộp bài thi: {Message}", 
                studentCode, message);

            return BadRequest(new { message });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Lỗi khi nộp bài thi cho sinh viên {StudentCode}", 
            User.FindFirst("studentCode")?.Value);

        return StatusCode(500, new { message = "Lỗi server khi nộp bài thi" });
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
    
    [HttpGet("grades/{examSessionSubjectId}")]
    [Authorize(Policy = "AdminOnly")]
    [Authorize(Policy = "LecturerOnly")]
    public async Task<IActionResult> GetStudentGrades(int examSessionSubjectId)
    {
        try
        {
            _logger.LogInformation("Yêu cầu lấy danh sách điểm cho ExamSessionSubjectId: {ExamSessionSubjectId}", examSessionSubjectId);
            
            var (grades, subjectCode) = await _studentService.GetStudentGradesByExamSessionSubjectAsync(examSessionSubjectId);
            
            return Ok(new { 
                success = true, 
                data = grades,
                subjectCode = subjectCode,
                count = grades.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách điểm cho ExamSessionSubjectId: {ExamSessionSubjectId}", examSessionSubjectId);
            return StatusCode(500, new { success = false, message = "Lỗi server khi lấy danh sách điểm" });
        }
    }
    
    [HttpGet("grades/{examSessionSubjectId}/export")]
    [Authorize(Policy = "LecturerOnly")]
    public async Task<IActionResult> ExportStudentGrades(int examSessionSubjectId)
    {
        try
        {
            _logger.LogInformation("Yêu cầu export Excel bảng điểm cho ExamSessionSubjectId: {ExamSessionSubjectId}", examSessionSubjectId);
            
            // Lấy dữ liệu điểm và mã môn học
            var (grades, subjectCode) = await _studentService.GetStudentGradesByExamSessionSubjectAsync(examSessionSubjectId);
            
            // Tạo file Excel
            var excelBytes = await _studentService.ExportStudentGradesToExcelAsync(grades);
            
            // Tạo tên file với mã môn học
            string fileName = $"BangDiem_{subjectCode}_ESS{examSessionSubjectId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            
            // Trả về file Excel
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi export Excel bảng điểm cho ExamSessionSubjectId: {ExamSessionSubjectId}", examSessionSubjectId);
            return StatusCode(500, new { success = false, message = "Lỗi server khi export bảng điểm" });
        }
    }

    [HttpPost("get-submission-result")]
    public async Task<IActionResult> GetSubmissionResult([FromBody] GetSubmissionResultRequest request)
    {
        try
        {
            var studentCode = User.FindFirst("studentCode")?.Value;
            if (string.IsNullOrEmpty(studentCode))
            {
                return Unauthorized();
            }

            if (request.StudentExamSessionId <= 0)
            {
                return BadRequest();
            }

            _logger.LogInformation("🔍 Sinh viên {StudentCode} yêu cầu lấy kết quả nộp bài cho phiên {SessionId}", 
                studentCode, request.StudentExamSessionId);

            var submissionData = await _studentService.GetSubmissionResultAsync(studentCode, request.StudentExamSessionId);

            if (submissionData != null)
            {
                _logger.LogInformation("✅ Lấy kết quả nộp bài thành công cho sinh viên {StudentCode}. Điểm: {Score}", 
                    studentCode, submissionData.Score);

                return Ok(submissionData);
            }
            else
            {
                _logger.LogWarning("⚠️ Không thể lấy kết quả nộp bài cho sinh viên {StudentCode}", studentCode);

                return NotFound();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi lấy kết quả nộp bài cho sinh viên {StudentCode}", 
                User.FindFirst("studentCode")?.Value);

            return StatusCode(500);
        }
    }
    
    [HttpPost("start-exam/test")]
    public async Task<IActionResult> StartExamtest()
    {
        try
        {
            var (result, pp, originalPaper) = await _studentService.StartExamAsync("012", 12);
            if (result == null) 
                return BadRequest(new { message = "Không thể bắt đầu làm bài vì không có phiên thi." });
            
            return Ok(new { studentSession = result, examPaper = pp, originalExamPaper = originalPaper });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi bắt đầu thi cho sinh viên");
            return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau." });
        }
    }
    
    [HttpGet("ExamSession-by-student-code")]
    public async Task<ActionResult<IEnumerable<StudentExamSessionHistoryDto>>> GetStudentExamSessionsByStudentCodeAsync()
    {
        var studentCode = User.FindFirst("studentCode")?.Value;
        if (string.IsNullOrEmpty(studentCode))
        {
            return Unauthorized(new { message = "Không tìm thấy thông tin sinh viên" });
        }
        var result = await _studentService.GetStudentExamSessionsByStudentCodeAsync(studentCode);
        return Ok(result);
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

public class GetSubmissionResultRequest
{
    public int StudentExamSessionId { get; set; }
}

public class LoginRequest
{
    public string StudentCode1 { get; set; }
    public string StudentCode2 { get; set; }
} 
