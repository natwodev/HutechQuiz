using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace backend_manage.Authentication
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenanceController : ControllerBase
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IConfiguration _config;

        public MaintenanceController(IConnectionMultiplexer redis, IConfiguration config)
        {
            _redis = redis;
            _config = config;
        }

        [HttpPost("maintenance")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ToggleMaintenance([FromQuery] string key, [FromQuery] bool enable)
        {
            var secretKey = _config["SecretAccess:SecretLoginKey"];

            if (key != secretKey)
                return Forbid("Bạn không có quyền thay đổi chế độ bảo trì.");

            var db = _redis.GetDatabase();
            // Xóa key cũ trong Redis trước khi thiết lập lại giá trị
            await db.KeyDeleteAsync("maintenance_mode");

            // Thiết lập giá trị mới
            await db.StringSetAsync("maintenance_mode", enable ? "true" : "false");

            return Ok(new { message = enable ? "Chế độ bảo trì đã được bật." : "Chế độ bảo trì đã được tắt." });
        }

        [HttpGet("maintenance")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CheckMaintenance()
        {
            var db = _redis.GetDatabase();
            var status = await db.StringGetAsync("maintenance_mode");
            return Ok(new { maintenance = status == "true" });
        }
    }
}

// POST: api/maintenance/maintenance?key=xxx&enable=true    → Bật/tắt chế độ bảo trì (cần key và quyền Admin)
// GET: api/maintenance/maintenance                         → Kiểm tra trạng thái chế độ bảo trì (cần quyền Admin)
