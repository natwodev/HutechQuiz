using AspNetCoreRateLimit;
using backend_manage.Middlewares;
using backend_manage.Hubs;

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
    }
}
