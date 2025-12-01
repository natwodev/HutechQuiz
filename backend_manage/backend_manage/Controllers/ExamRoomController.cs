using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;

namespace backend_manage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExamRoomController : ControllerBase
    {
        private readonly IExamRoomService _service;

        public ExamRoomController(IExamRoomService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound($"Không tìm thấy ExamRoom với ID: {id}");
            
            return Ok(result);
        }
    }
}

