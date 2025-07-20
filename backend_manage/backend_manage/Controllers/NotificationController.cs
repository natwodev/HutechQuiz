using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using backend_manage.Hubs;
using System.Threading.Tasks;
using System;

namespace backend_manage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        public NotificationController(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost("send-exam-reminder")]
        public async Task<IActionResult> SendExamReminder([FromBody] ExamReminderDto dto)
        {
            // Gửi tới tất cả client, có thể mở rộng gửi theo group/user
            await _hubContext.Clients.All.SendAsync("ExamReminder", dto.Message, dto.ExamTime);
            return Ok(new { message = "Đã gửi thông báo nhắc giờ làm bài." });
        }
    }

    public class ExamReminderDto
    {
        public string Message { get; set; }
        public DateTime ExamTime { get; set; }
    }
} 