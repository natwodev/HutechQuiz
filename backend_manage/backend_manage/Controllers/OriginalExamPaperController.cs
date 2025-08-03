using backend_manage.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

namespace backend_manage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OriginalExamPaperController : ControllerBase
    {
        private readonly IOriginalExamPaperService _originalExamPaperService;
        public OriginalExamPaperController(IOriginalExamPaperService originalExamPaperService)
        {
            _originalExamPaperService = originalExamPaperService;
        }

        [HttpPost("import-xml")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ImportXml([FromForm] IFormFile file,[FromForm] string originalExamPaperCore)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không hợp lệ hoặc rỗng");
            await _originalExamPaperService.ImportFromXmlAsync(file,originalExamPaperCore);
            return Ok(new { message = "Import thành công (nếu mã môn học chưa tồn tại)." });
        }
        
        [HttpPost("create-shuffled")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateShuffled([FromForm] string originalExamPaperCore, [FromForm] int count)
        {
            if (string.IsNullOrWhiteSpace(originalExamPaperCore) || count <= 0)
                return BadRequest("Thiếu mã đề thi gốc hoặc số lượng không hợp lệ");
            await _originalExamPaperService.CreateShuffledExamPapersAsync(originalExamPaperCore, count);
            return Ok(new { message = $"Đã tạo {count} đề thi hoán vị cho mã đề {originalExamPaperCore}" });
        }

        [HttpGet("{core}/with-details")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetWithDetails(string core)
        {
            var result = await _originalExamPaperService.GetWithDetailsAsync(core);
            if (result == null) return NotFound();
            return Ok(result);
        }
    }
}