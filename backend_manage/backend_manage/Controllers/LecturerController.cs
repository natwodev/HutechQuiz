using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using System.Security.Claims;
using backend_manage.core.Entities;
using backend_manage.core.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend_manage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LecturerController : ControllerBase
    {
        private readonly ILecturerService _lecturerService;
        private readonly IStudentService _studentService;
        private readonly IRepository<StudentExamSession> _studentExamSessionRepository;

        public LecturerController(
            ILecturerService lecturerService,
            IStudentService studentService,
            IRepository<StudentExamSession> studentExamSessionRepository)
        {
            _lecturerService = lecturerService;
            _studentService = studentService;
            _studentExamSessionRepository = studentExamSessionRepository;
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<LecturerDto>> AddLecturer([FromBody] LecturerCreateDto dto)
        {
            var result = await _lecturerService.AddLecturerAsync(dto);
            return CreatedAtAction(nameof(GetByLecturerCode), new { lecturerCode = result.LecturerCode }, result);
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<IEnumerable<LecturerDto>>> GetAllLecturers()
        {
            var result = await _lecturerService.GetAllLecturersAsync();
            return Ok(result);
        }

        [HttpGet("{lecturerCode}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<LecturerDto>> GetByLecturerCode(string lecturerCode)
        {
            var result = await _lecturerService.GetByLecturerCodeAsync(lecturerCode);
            if (result == null) return NotFound();
            return Ok(result);
        }

        // GET: api/lecturer/profile
        [HttpGet("profile")]
        [Authorize(Policy = "LecturerOnly")]
        public async Task<ActionResult<LecturerDto>> GetProfile()
        {
            try
            {
                var lecturerCode = User.FindFirst("lecturerCode")?.Value;
                if (string.IsNullOrEmpty(lecturerCode))
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin giảng viên trong token" });
                }

                var lecturer = await _lecturerService.GetProfileAsync(lecturerCode);
                if (lecturer == null)
                {
                    return NotFound(new { message = "Không tìm thấy thông tin giảng viên" });
                }

                return Ok(lecturer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server khi lấy thông tin profile" });
            }
        }

        // ============ Monitor features (merged) ============
        // Nộp bài thay cho một sinh viên
        [HttpPost("force-submit")]
        [Authorize(Policy = "LecturerOnly")]
        public async Task<IActionResult> ForceSubmit([FromBody] ForceSubmitRequest dto)
        {
            if (dto == null || dto.StudentExamSessionId <= 0 || string.IsNullOrWhiteSpace(dto.StudentCode))
            {
                return BadRequest(new { message = "Thiếu thông tin yêu cầu." });
            }

            var (success, message) = await _lecturerService.ForceSubmitAsync(
                dto.StudentExamSessionId,
                dto.StudentCode);
            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new { message = "Nộp bài thành công cho sinh viên" });
        }

        public class ForceSubmitRequest
        {
            public int StudentExamSessionId { get; set; }
            public string StudentCode { get; set; } = string.Empty;
        }
    }
} 