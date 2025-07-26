using System.Security.Claims;
using backend_manage.DTOs;
using backend_manage.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace backend_manage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamRoomLecturerAssignmentController : ControllerBase
    {
        private readonly IExamRoomLecturerAssignmentService _examRoomLecturerAssignmentService;
        public ExamRoomLecturerAssignmentController(IExamRoomLecturerAssignmentService examRoomLecturerAssignmentService)
        {
            _examRoomLecturerAssignmentService = examRoomLecturerAssignmentService;
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _examRoomLecturerAssignmentService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _examRoomLecturerAssignmentService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create([FromBody] ExamRoomLecturerAssignmentDto dto)
        {
            var result = await _examRoomLecturerAssignmentService.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.ExamRoomLecturerAssignmentId }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(string id, [FromBody] ExamRoomLecturerAssignmentDto dto)
        {
            var result = await _examRoomLecturerAssignmentService.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _examRoomLecturerAssignmentService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
} 