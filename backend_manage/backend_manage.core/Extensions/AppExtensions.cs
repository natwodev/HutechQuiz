using backend_manage.core.Hubs;
using backend_manage.core.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace backend_manage.core.Extensions
{
    public static class AppExtensions
    {
        public static void ConfigureMiddleware(this WebApplication app)
        {
            app.UseMiddleware<ExceptionMiddleware>(); // Xử lý lỗi chung
            
            app.Urls.Add("http://0.0.0.0:5163"); // Lắng nghe mọi IP

            app.UseCors("AllowAll"); // ✅ Dùng 1 lần, đúng policy

            // app.UseHttpsRedirection(); // Bật lại nếu dùng HTTPS
            
            app.UseStaticFiles(); // ✅ Bật để phục vụ file tĩnh từ wwwroot
            
            app.UseAuthentication();
            app.UseAuthorization();

            // app.UseIpRateLimiting(); // Tắt rate limiting

            app.MapControllers();

            app.MapHub<NotificationHub>("/notificationHub"); // ✅ SignalR Hub
        }
    }
}
