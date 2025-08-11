using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System;
using backend_manage.core.Hubs;
using Microsoft.AspNetCore.Authorization;

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
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", dto.Message, dto.ExamTime);
            return Ok(new { message = "Đã gửi thông báo nhắc giờ làm bài." });
        }
        
        
    }

    public class ExamReminderDto
    {
        public string Message { get; set; }
        public DateTime ExamTime { get; set; }
    }
    
} 