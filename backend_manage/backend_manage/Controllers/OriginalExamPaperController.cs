using backend_manage.DTOs;
using backend_manage.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace backend_manage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OriginalExamPaperController : ControllerBase
    {
        private readonly IOriginalExamPaperService _originalExamPaperService;
        public OriginalExamPaperController(IOriginalExamPaperService originalExamPaperService)
        {
            _originalExamPaperService = originalExamPaperService;
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _originalExamPaperService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _originalExamPaperService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create([FromBody] OriginalExamPaperDto dto)
        {
            var result = await _originalExamPaperService.AddAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.OriginalExamPaperId }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(string id, [FromBody] OriginalExamPaperDto dto)
        {
            var result = await _originalExamPaperService.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _originalExamPaperService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpPost("import-xml")]
        public async Task<IActionResult> ImportXml([FromForm] IFormFile file,[FromForm] string OriginalExamPaperCore)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không hợp lệ hoặc rỗng");
            await _originalExamPaperService.ImportFromXmlAsync(file,OriginalExamPaperCore);
            return Ok(new { message = "Import thành công (nếu mã môn học chưa tồn tại)." });
        }
        
        [HttpPost("create-shuffled")]
        public async Task<IActionResult> CreateShuffled([FromForm] string originalExamPaperCore, [FromForm] int count)
        {
            if (string.IsNullOrWhiteSpace(originalExamPaperCore) || count <= 0)
                return BadRequest("Thiếu mã đề thi gốc hoặc số lượng không hợp lệ");
            await _originalExamPaperService.CreateShuffledExamPapersAsync(originalExamPaperCore, count);
            return Ok(new { message = $"Đã tạo {count} đề thi hoán vị cho mã đề {originalExamPaperCore}" });
        }

        [HttpGet("{core}/with-details")]
        public async Task<IActionResult> GetWithDetails(string core)
        {
            var result = await _originalExamPaperService.GetWithDetailsAsync(core);
            if (result == null) return NotFound();
            return Ok(result);
        }
    }
} 