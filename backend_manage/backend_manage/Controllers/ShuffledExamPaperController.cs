using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using backend_manage.core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace backend_manage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShuffledExamPaperController : ControllerBase
    {
        private readonly IShuffledExamPaperService _service;
        private readonly ILogger<ShuffledExamPaperController> _logger;

        public ShuffledExamPaperController(IShuffledExamPaperService service, ILogger<ShuffledExamPaperController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("{core}/with-details")]
        [Authorize(Policy = "ExamManagement")]
        public async Task<IActionResult> GetWithDetails(string core)
        {
            var result = await _service.GetWithDetailsAsync(core);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("by-original")]
        [Authorize(Policy = "ExamManagement")]
        public async Task<IActionResult> GetByOriginalExamPaperCore([FromQuery] string originalExamPaperCore)
        {
            try
            {
                var result = await _service.GetByOriginalExamPaperCoreAsync(originalExamPaperCore);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách đề hoán vị cho mã đề gốc {Core}", originalExamPaperCore);
                return StatusCode(500, new { message = $"Lỗi khi lấy danh sách đề hoán vị: {ex.Message}" });
            }
        }

        [HttpPut("{core}/allow-view-materials")]
        [Authorize(Policy = "ExamManagement")]
        public async Task<IActionResult> UpdateAllowViewMaterials(string core, [FromBody] UpdateAllowViewMaterialsRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(core))
                    return BadRequest("Mã đề hoán vị không hợp lệ");

                var result = await _service.UpdateAllowViewMaterialsAsync(core, request.AllowViewMaterials);
                if (result)
                {
                    _logger.LogInformation("Đã cập nhật AllowViewMaterials = {Value} cho đề hoán vị {Core}", 
                        request.AllowViewMaterials, core);
                    return Ok(new { message = $"Đã cập nhật AllowViewMaterials = {request.AllowViewMaterials} cho đề hoán vị" });
                }
                return BadRequest("Không thể cập nhật AllowViewMaterials");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật AllowViewMaterials cho đề hoán vị {Core}", core);
                return StatusCode(500, new { message = $"Lỗi khi cập nhật AllowViewMaterials: {ex.Message}" });
            }
        }
        
        public class UpdateAllowViewMaterialsRequest
        {
            public bool AllowViewMaterials { get; set; }
        }
    
    }
} 