using backend_manage.DTOs;
using backend_manage.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace backend_manage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamSessionSubjectController : ControllerBase
    {
        private readonly IExamSessionSubjectService _examSessionSubjectService;
        public ExamSessionSubjectController(IExamSessionSubjectService examSessionSubjectService)
        {
            _examSessionSubjectService = examSessionSubjectService;
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _examSessionSubjectService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _examSessionSubjectService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create([FromBody] ExamSessionSubjectCreateDto dto)
        {
            var result = await _examSessionSubjectService.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.ExamSessionSubjectId }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(string id, [FromBody] ExamSessionSubjectUpdateDto dto)
        {
            var result = await _examSessionSubjectService.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _examSessionSubjectService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpPatch("{id}/original-exam-paper/{originalExamPaperId}")]
        public async Task<IActionResult> UpdateOriginalExamPaperId(int id, int originalExamPaperId)
        {
            var success = await _examSessionSubjectService.UpdateOriginalExamPaperIdAsync(id, originalExamPaperId);
            if (!success) return NotFound();
            return Ok(new { message = "Cập nhật OriginalExamPaperId thành công." });
        }

        [HttpGet("with-rooms")]
        public async Task<IActionResult> GetAllWithRooms()
        {
            var result = await _examSessionSubjectService.GetAllWithRoomsAsync();
            return Ok(result);
        }
    }
} 