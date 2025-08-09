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
    public class ExamSessionDepartmentController : ControllerBase
    {
        private readonly IExamSessionDepartmentService _examSessionDepartmentService;
        public ExamSessionDepartmentController(IExamSessionDepartmentService examSessionDepartmentService)
        {
            _examSessionDepartmentService = examSessionDepartmentService;
        }

        [HttpGet]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _examSessionDepartmentService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _examSessionDepartmentService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> Create([FromBody] ExamSessionDepartmentCreateDto dto)
        {
            var result = await _examSessionDepartmentService.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.ExamSessionDepartmentId }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> Update(string id, [FromBody] ExamSessionDepartmentUpdateDto dto)
        {
            var result = await _examSessionDepartmentService.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _examSessionDepartmentService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
} 