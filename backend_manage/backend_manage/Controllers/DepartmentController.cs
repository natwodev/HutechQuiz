using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using backend_manage.shared.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_manage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;
        public DepartmentController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetAll()
        {
            var result = await _departmentService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<DepartmentDto>> GetById(string id)
        {
            var result = await _departmentService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<DepartmentDto>> Create([FromBody] DepartmentCreateDto dto)
        {
            var result = await _departmentService.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.DepartmentId }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<DepartmentDto>> Update(string id, [FromBody] DepartmentUpdateDto dto)
        {
            var result = await _departmentService.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _departmentService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
} 