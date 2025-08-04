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

    
    }
} 