using System.Security.Claims;
using backend_manage.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

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