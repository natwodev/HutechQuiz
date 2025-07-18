using backend_manage.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

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
        public async Task<IActionResult> ImportXml([FromForm] IFormFile file,[FromForm] string OriginalExamPaperCore)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không hợp lệ hoặc rỗng");
            await _originalExamPaperService.ImportFromXmlAsync(file,OriginalExamPaperCore);
            return Ok(new { message = "Import thành công (nếu mã môn học chưa tồn tại)." });
        }
    }
} 