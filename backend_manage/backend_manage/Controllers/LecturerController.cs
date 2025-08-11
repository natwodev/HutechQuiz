using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using backend_manage.shared.Interfaces;
using System.Security.Claims;

namespace backend_manage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LecturerController : ControllerBase
    {
        private readonly ILecturerService _lecturerService;
        public LecturerController(ILecturerService lecturerService)
        {
            _lecturerService = lecturerService;
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
    }
} 