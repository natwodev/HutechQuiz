using AspNetCoreRateLimit;
using backend_manage.Middlewares;
using backend_manage.Hubs;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace backend_manage.Extensions
{
    public static class AppExtensions
    {
        public static void ConfigureMiddleware(this WebApplication app)
        {
            app.UseMiddleware<ExceptionMiddleware>(); // Xử lý lỗi chung

            app.UseMiddleware<MaintenanceMiddleware>(); // Check chế độ bảo trì

            app.Urls.Add("http://0.0.0.0:5163"); // Lắng nghe mọi IP

            app.UseCors("AllowAll"); // ✅ Dùng 1 lần, đúng policy

            // app.UseHttpsRedirection(); // Bật lại nếu dùng HTTPS

            app.UseMiddleware<JwtBlacklistMiddleware>(); // Check token trong blacklist

            app.UseAuthentication();
            app.UseAuthorization();

            // app.UseStaticFiles(); // Bật nếu có phục vụ file tĩnh (ảnh, js...)

            app.UseIpRateLimiting();

            app.MapControllers();

            app.MapHub<NotificationHub>("/notificationHub"); // ✅ SignalR Hub
        }

        public static bool IsRedisConnected(this IConnectionMultiplexer redis, ILogger logger)
        {
            try
            {
                var db = redis.GetDatabase();
                // Thử ping để kiểm tra kết nối thực sự
                return db.Ping().TotalSeconds < 1;
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
