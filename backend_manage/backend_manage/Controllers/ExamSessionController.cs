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
    public class ExamSessionController : ControllerBase
    {
        private readonly IExamSessionService _service;

        public ExamSessionController(IExamSessionService service)
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
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> Create([FromBody] ExamSessionCreateDto dto)
        {
            var result = await _service.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.ExamSessionId }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> Update(string id, [FromBody] ExamSessionUpdateDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
} 