using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using backend_manage.core.Services.Interfaces;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace backend_manage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OriginalExamPaperController : ControllerBase
    {
        private readonly IOriginalExamPaperService _originalExamPaperService;
        private readonly ILogger<OriginalExamPaperController> _logger;
        
        public OriginalExamPaperController(
            IOriginalExamPaperService originalExamPaperService,
            ILogger<OriginalExamPaperController> logger)
        {
            _originalExamPaperService = originalExamPaperService;
            _logger = logger;
        }

        [HttpPost("import-xml")]
        [Authorize(Policy = "ExamManagement")]
        public async Task<IActionResult> ImportXml([FromForm] IFormFile file,[FromForm] string originalExamPaperCore)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File không hợp lệ hoặc rỗng");
            
            try
            {
                await _originalExamPaperService.ImportFromXmlAsync(file, originalExamPaperCore);
                _logger.LogInformation("Import đề thi thành công với mã: {OriginalExamPaperCore}", originalExamPaperCore);
                return Ok(new { message = "Import thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi import đề thi với mã: {OriginalExamPaperCore}. Lỗi: {ErrorMessage}", 
                    originalExamPaperCore, ex.Message);
                
                // Trả về lỗi với thông báo rõ ràng
                if (ex.Message.Contains("Đã tồn tại đề thi"))
                {
                    return BadRequest(new { message = ex.Message });
                }
                
                return StatusCode(500, new { message = $"Lỗi khi import đề thi: {ex.Message}" });
            }
        }
        
        [HttpGet("{core}/with-details")]
        [Authorize(Policy = "ExamManagement")]
        public async Task<IActionResult> GetWithDetails(string core)
        {
            var result = await _originalExamPaperService.GetWithDetailsAsync(core);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("list")]
        [Authorize(Policy = "AcademicManagement")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _originalExamPaperService.GetAllAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách đề thi gốc");
                return StatusCode(500, new { message = $"Lỗi khi lấy danh sách đề thi: {ex.Message}" });
            }
        }
        
        
        
        [HttpPost("create-shuffled")]
        [Authorize(Policy = "ExamManagement")]
        public async Task<IActionResult> CreateShuffled([FromForm] string originalExamPaperCore, [FromForm] int count)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(originalExamPaperCore) || count <= 0)
                    return BadRequest("Thiếu mã đề thi gốc hoặc số lượng không hợp lệ");
                
                _logger.LogInformation("Bắt đầu tạo {Count} đề hoán vị cho mã đề {Core}", count, originalExamPaperCore);
                await _originalExamPaperService.CreateShuffledExamPapersAsync(originalExamPaperCore, count);
                _logger.LogInformation("Đã tạo thành công {Count} đề hoán vị cho mã đề {Core}", count, originalExamPaperCore);
                return Ok(new { message = $"Đã tạo {count} đề thi hoán vị cho mã đề {originalExamPaperCore}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo đề hoán vị cho mã đề {Core}, số lượng {Count}", originalExamPaperCore, count);
                return StatusCode(500, new { message = $"Lỗi khi tạo đề hoán vị: {ex.Message}" });
            }
        }

        [HttpPut("{core}/allow-view-materials")]
        [Authorize(Policy = "ExamManagement")]
        public async Task<IActionResult> UpdateAllowViewMaterials(string core, [FromBody] UpdateAllowViewMaterialsRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(core))
                    return BadRequest("Mã đề thi gốc không hợp lệ");

                var result = await _originalExamPaperService.UpdateAllowViewMaterialsAsync(core, request.AllowViewMaterials);
                if (result)
                {
                    _logger.LogInformation("Đã cập nhật AllowViewMaterials = {Value} cho đề thi gốc {Core} và các đề hoán vị liên quan", 
                        request.AllowViewMaterials, core);
                    return Ok(new { message = $"Đã cập nhật AllowViewMaterials = {request.AllowViewMaterials} cho đề thi gốc và các đề hoán vị liên quan" });
                }
                return BadRequest("Không thể cập nhật AllowViewMaterials");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật AllowViewMaterials cho đề thi gốc {Core}", core);
                return StatusCode(500, new { message = $"Lỗi khi cập nhật AllowViewMaterials: {ex.Message}" });
            }
        }
        
        public class UpdateAllowViewMaterialsRequest
        {
            public bool AllowViewMaterials { get; set; }
        }
        
        /*
        
       
        
        [HttpGet("by-subject/{subjectId}")]
        [Authorize]
        public async Task<IActionResult> GetOriginalExamDtosBySubjectId(int subjectId)
        {
            try
            {
                var originalExamDtos = await _originalExamPaperService.GetOriginalExamDtosBySubjectIdAsync(subjectId);
                return Ok(originalExamDtos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi lấy danh sách đề thi: {ex.Message}" });
            }
        }
        
   
        
        [HttpGet("{originalExamPaperId}/parent-questions")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetParentQuestions(int originalExamPaperId)
        {
            try
            {
                var result = await _originalExamPaperService.GetParentQuestionsAsync(originalExamPaperId);
                return Ok(new
                {
                    shufflableQuestions = result.ShufflableQuestions,
                    nonShufflableQuestions = result.NonShufflableQuestions,
                    totalShufflable = result.ShufflableQuestions.Count(),
                    totalNonShufflable = result.NonShufflableQuestions.Count()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi lấy danh sách câu hỏi theo khả năng hoán vị: {ex.Message}" });
            }
        }

        [HttpGet("detail/{originalExamPaperDetailId}/child-questions")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetChildQuestions(int originalExamPaperDetailId)
        {
            try
            {
                var result = await _originalExamPaperService.GetChildQuestionsAsync(originalExamPaperDetailId);
                return Ok(new
                {
                    shufflableQuestions = result.ShufflableQuestions,
                    nonShufflableQuestions = result.NonShufflableQuestions,
                    totalShufflable = result.ShufflableQuestions.Count(),
                    totalNonShufflable = result.NonShufflableQuestions.Count()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi lấy danh sách câu hỏi con theo khả năng hoán vị: {ex.Message}" });
            }
        }
        
        [HttpGet("{originalExamPaperId}/child-questions")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetChildQuestionsByExamPaper(int originalExamPaperId)
        {
            try
            {
                var result = await _originalExamPaperService.GetChildQuestionsByExamPaperAsync(originalExamPaperId);
                return Ok(new
                {
                    shufflableQuestions = result.ShufflableQuestions,
                    nonShufflableQuestions = result.NonShufflableQuestions,
                    totalShufflable = result.ShufflableQuestions.Count(),
                    totalNonShufflable = result.NonShufflableQuestions.Count()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi lấy danh sách câu hỏi con theo khả năng hoán vị: {ex.Message}" });
            }
        }
        
        [HttpGet("{originalExamPaperId}/shuffle-parent-questions")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ShuffleParentQuestions(int originalExamPaperId)
        {
            try
            {
                var result = await _originalExamPaperService.ShuffleParentQuestionsAsync(originalExamPaperId);
                return Ok(new
                {
                    shuffledQuestions = result,
                    totalQuestions = result.Count,
                    message = "Đã hoán vị thành công các câu hỏi cha có thể hoán vị"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi hoán vị câu hỏi cha: {ex.Message}" });
            }
        }
        */
    }
}