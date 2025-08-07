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
            // Gửi tới tất cả client, có thể mở rộng gửi theo group/user
            await _hubContext.Clients.All.SendAsync("ExamReminder", dto.Message, dto.ExamTime);
            return Ok(new { message = "Đã gửi thông báo nhắc giờ làm bài." });
        }

        [HttpPost("logout-all-students-in-room")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> LogoutAllStudentsInRoom([FromBody] LogoutAllStudentsDto dto)
        {
            try
            {
                await _hubContext.Clients.Group($"room_{dto.ExamRoomId}").SendAsync("ForceLogout", new
                {
                    reason = dto.Reason,
                    examRoomId = dto.ExamRoomId,
                    timestamp = DateTime.UtcNow
                });

                return Ok(new { 
                    message = $"Đã logout toàn bộ sinh viên trong phòng thi {dto.ExamRoomId}",
                    examRoomId = dto.ExamRoomId,
                    reason = dto.Reason
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("logout-student")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> LogoutStudent([FromBody] LogoutStudentDto dto)
        {
            try
            {
                await _hubContext.Clients.Group($"room_{dto.ExamRoomId}").SendAsync("ForceLogout", new
                {
                    reason = dto.Reason,
                    examRoomId = dto.ExamRoomId,
                    studentCode = dto.StudentCode,
                    timestamp = DateTime.UtcNow
                });

                return Ok(new { 
                    message = $"Đã logout sinh viên {dto.StudentCode} khỏi phòng thi {dto.ExamRoomId}",
                    examRoomId = dto.ExamRoomId,
                    studentCode = dto.StudentCode,
                    reason = dto.Reason
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("students-in-room/{examRoomId}")]
        public async Task<IActionResult> GetStudentsInRoom(int examRoomId)
        {
            try
            {
                // Gọi method trong hub để lấy danh sách sinh viên
                await _hubContext.Clients.All.SendAsync("GetStudentsInRoom", examRoomId);
                
                return Ok(new { 
                    message = "Đã gửi yêu cầu lấy danh sách sinh viên trong phòng",
                    examRoomId = examRoomId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("send-room-notification")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> SendRoomNotification([FromBody] RoomNotificationDto dto)
        {
            try
            {
                await _hubContext.Clients.Group($"room_{dto.ExamRoomId}").SendAsync("RoomNotification", new
                {
                    message = dto.Message,
                    type = dto.Type, // "info", "warning", "error", "success"
                    examRoomId = dto.ExamRoomId,
                    timestamp = DateTime.UtcNow
                });

                return Ok(new { 
                    message = "Đã gửi thông báo đến phòng thi",
                    examRoomId = dto.ExamRoomId,
                    notificationType = dto.Type
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class ExamReminderDto
    {
        public string Message { get; set; }
        public DateTime ExamTime { get; set; }
    }

    public class LogoutAllStudentsDto
    {
        public int ExamRoomId { get; set; }
        public string Reason { get; set; } = "Admin logout";
    }

    public class LogoutStudentDto
    {
        public int ExamRoomId { get; set; }
        public string StudentCode { get; set; }
        public string Reason { get; set; } = "Admin logout";
    }

    public class RoomNotificationDto
    {
        public int ExamRoomId { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } = "info"; // "info", "warning", "error", "success"
    }
} 