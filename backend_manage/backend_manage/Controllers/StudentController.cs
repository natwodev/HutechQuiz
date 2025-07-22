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

namespace backend_manage.Controllers;

[ApiController]
[Route("api/[controller]")]

public class StudentController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentController(IStudentService studentService)
    {
        _studentService = studentService;
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
    public async Task<IActionResult> StartExam([FromForm] int examSessionSubjectId)
    {
        var studentCode = User.FindFirst("studentCode")?.Value;
        if (string.IsNullOrEmpty(studentCode))
            return Unauthorized(new { message = "Token không hợp lệ!" });
        var result = await _studentService.StartExamAsync(studentCode, examSessionSubjectId);
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
}


public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
} 
