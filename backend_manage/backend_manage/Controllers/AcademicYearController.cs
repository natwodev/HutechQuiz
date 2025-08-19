using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_manage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AcademicYearController : ControllerBase
    {
        private readonly IAcademicYearService _academicYearService;

        public AcademicYearController(IAcademicYearService academicYearService)
        {
            _academicYearService = academicYearService;
        }
       
        [HttpGet]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _academicYearService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _academicYearService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }
        
        #region Post Methods 
        [HttpPost]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> Create([FromBody] AcademicYearCreateDto dto)
        {
            var result = await _academicYearService.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.AcademicYearId }, result);
        }
        #endregion

        [HttpPut("{id}")]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> Update(int id, [FromBody] AcademicYearUpdateDto dto)
        {
            var result = await _academicYearService.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AcademicAffairsOrAdmin")]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _academicYearService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
} 