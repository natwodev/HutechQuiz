using backend_manage.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using backend_manage.Services.AuthService.Helpers;

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
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetWithDetails(string core)
        {
            var result = await _service.GetWithDetailsAsync(core);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost("preload-redis")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> PreloadToRedis()
        {
            await _service.PreloadApprovedPapersToRedisAsync();
            return Ok(new { message = "Đã tải sẵn tất cả đề thi hoán vị đã phê duyệt vào Redis" });
        }

        [HttpPost("preload-approved-papers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PreloadApprovedPapers()
        {
            try
            {
                var examPaperHelper = HttpContext.RequestServices.GetRequiredService<ExamPaperHelper>();
                await examPaperHelper.PreloadAllApprovedPapersAsync();
                
                return Ok(new { 
                    Success = true, 
                    Message = "Đã preload tất cả approved papers vào Redis cache thành công" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi preload approved papers");
                return StatusCode(500, new { 
                    Success = false, 
                    Message = "Lỗi khi preload approved papers: " + ex.Message 
                });
            }
        }

        [HttpGet("test-cache/{originalExamPaperId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestCache(int originalExamPaperId)
        {
            try
            {
                var examPaperHelper = HttpContext.RequestServices.GetRequiredService<ExamPaperHelper>();
                
                // Test lấy random paper từ cache
                var randomPaper = await examPaperHelper.GetRandomExamPaperAsync(originalExamPaperId);
                
                if (randomPaper != null)
                {
                    return Ok(new { 
                        Success = true, 
                        Message = "Test cache thành công",
                        ShuffledExamPaperId = randomPaper.ShuffledExamPaperId,
                        Title = randomPaper.Title
                    });
                }
                else
                {
                    return NotFound(new { 
                        Success = false, 
                        Message = "Không tìm thấy đề thi cho OriginalExamPaperId này" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi test cache");
                return StatusCode(500, new { 
                    Success = false, 
                    Message = "Lỗi khi test cache: " + ex.Message 
                });
            }
        }
    }
} 