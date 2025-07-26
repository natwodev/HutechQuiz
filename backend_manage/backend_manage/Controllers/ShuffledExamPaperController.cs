using backend_manage.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace backend_manage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShuffledExamPaperController : ControllerBase
    {
        private readonly IShuffledExamPaperService _service;
        public ShuffledExamPaperController(IShuffledExamPaperService service)
        {
            _service = service;
        }

        [HttpGet("{core}/with-details")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetWithDetails(string core)
        {
            var result = await _service.GetWithDetailsAsync(core);
            if (result == null) return NotFound();
            return Ok(result);
        }
    }
} 