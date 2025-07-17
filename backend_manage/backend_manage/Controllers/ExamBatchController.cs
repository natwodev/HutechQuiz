using backend_manage.DTOs;
using backend_manage.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_manage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExamBatchController : ControllerBase
    {
        private readonly IExamBatchService _examBatchService;

        public ExamBatchController(IExamBatchService examBatchService)
        {
            _examBatchService = examBatchService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _examBatchService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _examBatchService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ExamBatchCreateDto dto)
        {
            var result = await _examBatchService.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.ExamBatchId }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] ExamBatchUpdateDto dto)
        {
            var result = await _examBatchService.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _examBatchService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpPatch("{id}/toggle-active")]
        public async Task<IActionResult> ToggleIsActive(string id)
        {
            var success = await _examBatchService.ToggleIsActiveAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
} 