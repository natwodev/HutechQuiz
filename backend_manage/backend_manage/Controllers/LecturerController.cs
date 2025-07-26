using Microsoft.AspNetCore.Mvc;
using backend_manage.Services.Interfaces;
using backend_manage.DTOs;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

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
    }
} 