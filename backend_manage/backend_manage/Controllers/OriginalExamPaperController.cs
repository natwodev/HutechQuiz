using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using backend_manage.core.Services.Interfaces;
using System;
using System.Linq;

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
        
        [HttpGet("{core}/with-details")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetWithDetails(string core)
        {
            var result = await _originalExamPaperService.GetWithDetailsAsync(core);
            if (result == null) return NotFound();
            return Ok(result);
        }
        /*
        
        [HttpPost("create-shuffled")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateShuffled([FromForm] string originalExamPaperCore, [FromForm] int count)
        {
            if (string.IsNullOrWhiteSpace(originalExamPaperCore) || count <= 0)
                return BadRequest("Thiếu mã đề thi gốc hoặc số lượng không hợp lệ");
            await _originalExamPaperService.CreateShuffledExamPapersAsync(originalExamPaperCore, count);
            return Ok(new { message = $"Đã tạo {count} đề thi hoán vị cho mã đề {originalExamPaperCore}" });
        }
        
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