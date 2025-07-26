using Microsoft.AspNetCore.Mvc;
using backend_manage.Services.Interfaces;
using backend_manage.DTOs;
using System.Threading.Tasks;

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
        public async Task<IActionResult> AddLecturer([FromBody] LecturerCreateDto dto)
        {
            var result = await _lecturerService.AddLecturerAsync(dto);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLecturers()
        {
            var result = await _lecturerService.GetAllLecturersAsync();
            return Ok(result);
        }

        [HttpGet("{lecturerCode}")]
        public async Task<IActionResult> GetByLecturerCode(string lecturerCode)
        {
            var result = await _lecturerService.GetByLecturerCodeAsync(lecturerCode);
            if (result == null) return NotFound();
            return Ok(result);
        }
    }
} 