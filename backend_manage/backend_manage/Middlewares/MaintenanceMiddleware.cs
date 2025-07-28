using StackExchange.Redis;

namespace backend_manage.Middlewares;

public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<MaintenanceMiddleware> _logger;

    public MaintenanceMiddleware(RequestDelegate next, IConnectionMultiplexer redis, ILogger<MaintenanceMiddleware> logger)
    {
        _next = next;
        _redis = redis;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string maintenanceFlag = "false"; // Default is no maintenance

        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync("maintenance_mode");
            if (!value.IsNull)
            {
                maintenanceFlag = value.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi kiểm tra chế độ bảo trì từ Redis");
        }

        if (maintenanceFlag == "true" && 
            !context.Request.Path.StartsWithSegments("/api/auth/secret-login") && 
            !context.Request.Path.StartsWithSegments("/api/admin/maintenance"))
        {
            _logger.LogWarning("System is in maintenance mode, request denied: {Path}", context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new {
                message = "Hệ thống đang bảo trì, vui lòng quay lại sau."
            });
            return;
        }

        await _next(context);
    }
}