using System.Linq;
using backend_manage.core.Hubs;
using backend_manage.core.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace backend_manage.core.Extensions
{
    public class AuthenticationDebugLogger { } // Class for logger category
    
    public static class AppExtensions
    {
        public static void ConfigureMiddleware(this WebApplication app)
        {
            app.UseMiddleware<ExceptionMiddleware>(); // Xử lý lỗi chung
            
            app.Urls.Add("http://0.0.0.0:5163"); // Lắng nghe mọi IP

            app.UseCors("AllowAll"); // ✅ Dùng 1 lần, đúng policy

            // app.UseHttpsRedirection(); // Bật lại nếu dùng HTTPS
            
            app.UseStaticFiles(); // ✅ Bật để phục vụ file tĩnh từ wwwroot
            
            // Thêm middleware debug authentication
            app.Use(async (context, next) =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<AuthenticationDebugLogger>>();
                
                logger.LogInformation("🚀 Request: {Method} {Path}", context.Request.Method, context.Request.Path);
                logger.LogInformation("🍪 Cookies received: {Cookies}", 
                    string.Join(", ", context.Request.Cookies.Select(c => $"{c.Key}={c.Value?.Substring(0, Math.Min(20, c.Value.Length))}...")));
                
                await next();
                
                logger.LogInformation("🔐 User authenticated: {IsAuthenticated}", context.User?.Identity?.IsAuthenticated ?? false);
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    var roles = context.User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
                    logger.LogInformation("👤 User: {UserName}, Roles: {Roles}", 
                        context.User.Identity.Name, string.Join(", ", roles));
                }
            });
            
            app.UseAuthentication();
            app.UseAuthorization();

            // app.UseIpRateLimiting(); // Tắt rate limiting

            app.MapControllers();

            app.MapHub<NotificationHub>("/notificationHub"); // ✅ SignalR Hub
        }

        public static bool IsRedisConnected(this IConnectionMultiplexer redis, ILogger logger)
        {
            try
            {
                // Chỉ kiểm tra trạng thái connection, không ping
                return redis.IsConnected;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis không khả dụng");
                return false;
            }
        }

        public static IConnectionMultiplexer? ConfigureRedis(this IServiceProvider serviceProvider, string connectionString, ILogger logger)
        {
            try
            {
                var redisConfig = ConfigurationOptions.Parse(connectionString);
                redisConfig.AbortOnConnectFail = false;
                
                logger.LogInformation("Đang kết nối đến Redis server...");
                var redis = ConnectionMultiplexer.Connect(redisConfig);
                
                // Kiểm tra kết nối thực sự
                if (redis.IsRedisConnected(logger))
                {
                    logger.LogInformation("Kết nối Redis thành công!");
                    return redis;
                }
                else
                {
                    logger.LogWarning("Không thể ping đến Redis server");
                    return null;
                }
            }
            catch (RedisConnectionException ex)
            {
                logger.LogError(ex, "⚠️ Không thể kết nối đến Redis server. Chi tiết lỗi: {ErrorMessage}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "⚠️ Có lỗi xảy ra khi kết nối đến Redis. Chi tiết lỗi: {ErrorMessage}", ex.Message);
                return null;
            }
        }
    }
}
