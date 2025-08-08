using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace backend_manage.core.Middlewares;

public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MaintenanceMiddleware> _logger;

    public MaintenanceMiddleware(RequestDelegate next, ILogger<MaintenanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Middleware đã được đơn giản hóa - chỉ pass through
        await _next(context);
    }
}