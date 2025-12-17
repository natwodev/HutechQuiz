using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_manage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubjectController : ControllerBase
    {
        private readonly ISubjectService _subjectService;
        
        public SubjectController(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        [HttpGet]
        [Authorize(Policy = "ExamManagement")]
        public async Task<ActionResult<IEnumerable<SubjectDto>>> GetAll()
        {
            var result = await _subjectService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<SubjectDto>> GetById(int id)
        {
            var result = await _subjectService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<SubjectDto>> Create([FromBody] SubjectCreateDto dto)
        {
            var result = await _subjectService.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.SubjectId }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<SubjectDto>> Update(int id, [FromBody] SubjectUpdateDto dto)
        {
            var result = await _subjectService.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _subjectService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}

