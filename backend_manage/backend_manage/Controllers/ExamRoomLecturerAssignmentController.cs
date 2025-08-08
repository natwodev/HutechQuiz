using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.Interfaces;

namespace backend_manage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExamRoomLecturerAssignmentController : ControllerBase
    {
        private readonly IExamRoomLecturerAssignmentService _service;
        public ExamRoomLecturerAssignmentController(IExamRoomLecturerAssignmentService service)
        {
            _service = service;
        }

        [HttpGet("my-assignments")]
        public async Task<IActionResult> GetRoomsByLecturer()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Token không hợp lệ!" });

            var result = await _service.GetByLecturerIdAsync(userId);
            return Ok(result);
        }

    }
}