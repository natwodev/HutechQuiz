using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using backend_manage.shared.Interfaces;

namespace backend_manage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamBatchDetailController : ControllerBase
    {
        private readonly IExamBatchDetailService _examBatchDetailService;
        public ExamBatchDetailController(IExamBatchDetailService examBatchDetailService)
        {
            _examBatchDetailService = examBatchDetailService;
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _examBatchDetailService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _examBatchDetailService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create([FromBody] ExamBatchDetailCreateDto dto)
        {
            var result = await _examBatchDetailService.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.ExamBatchDetailId }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(string id, [FromBody] ExamBatchDetailUpdateDto dto)
        {
            var result = await _examBatchDetailService.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _examBatchDetailService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
} 