using backend_manage.core.Messages.RabbitMQ;
using Microsoft.AspNetCore.Mvc;

namespace backend_manage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueueStatusController : ControllerBase
    {
        private readonly IRabbitMqService _rabbitMQService;
        private readonly ILogger<QueueStatusController> _logger;

        public QueueStatusController(IRabbitMqService rabbitMQService, ILogger<QueueStatusController> logger)
        {
            _rabbitMQService = rabbitMQService;
            _logger = logger;
        }

        [HttpGet("check")]
        public IActionResult CheckQueues()
        {
            try
            {
                _logger.LogInformation("Kiểm tra trạng thái các queues...");
                
                _rabbitMQService.CheckQueueStatus("student_answer_saved_queue");
                _rabbitMQService.CheckQueueStatus("exam_submission_queue");
                
                return Ok(new { Message = "Đã kiểm tra trạng thái queues" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra queue status");
                return StatusCode(500, new { Error = "Lỗi khi kiểm tra queue status" });
            }
        }
    }
} 