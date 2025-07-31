using Microsoft.Extensions.Logging;
using backend_manage.Services.Interfaces;

namespace backend_manage.Middlewares;

public class JwtBlacklistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JwtBlacklistMiddleware> _logger;

    public JwtBlacklistMiddleware(RequestDelegate next, IServiceProvider serviceProvider, ILogger<JwtBlacklistMiddleware> logger)
    {
        _next = next;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                // Sử dụng IServiceProvider để resolve IRedisService khi cần
                using var scope = _serviceProvider.CreateScope();
                var redisService = scope.ServiceProvider.GetService<IRedisService>();
                
                if (redisService != null && redisService.IsConnected)
                {
                    var isRevoked = await redisService.StringGetAsync($"blacklist:{token}");
                    if (!string.IsNullOrEmpty(isRevoked))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Token has been revoked");
                        return;
                    }
                }
                else
                {
                    _logger.LogWarning("Redis không khả dụng, bỏ qua kiểm tra blacklist token");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra blacklist token");
                // Tiếp tục xử lý request mà không kiểm tra blacklist
            }
        }

        await _next(context);
    }
}