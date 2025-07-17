using backend_manage.DTOs;
using backend_manage.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_manage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AcademicYearController : ControllerBase
    {
        private readonly IAcademicYearService _academicYearService;

        public AcademicYearController(IAcademicYearService academicYearService)
        {
            _academicYearService = academicYearService;
        }
       
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _academicYearService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _academicYearService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }
        
        #region Post Methods 
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AcademicYearCreateDto dto)
        {
            var result = await _academicYearService.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.AcademicYearId }, result);
        }
        #endregion

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] AcademicYearUpdateDto dto)
        {
            var result = await _academicYearService.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _academicYearService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
} 