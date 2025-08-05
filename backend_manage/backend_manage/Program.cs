using backend_manage.DTOs;
using backend_manage.Extensions;
using OfficeOpenXml;
using Serilog;
using Microsoft.EntityFrameworkCore;
using backend_manage.Messages.RabbitMQ;
using backend_manage.Services.AuthService.Helpers;
using StackExchange.Redis;



var builder = WebApplication.CreateBuilder(args);

// Cấu hình Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Cấu hình Kestrel server cho high concurrency và tăng timeout
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 2000; // Tăng số connection đồng thời
    options.Limits.MaxConcurrentUpgradedConnections = 2000;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5); // Tăng keep-alive
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(60); // Tăng timeout
    
    // Cấu hình thread pool
    options.Limits.MaxRequestBufferSize = 1024 * 1024; // 1MB
    options.Limits.MaxRequestLineSize = 8192; // 8KB
    
    // Tối ưu cho performance
    options.AllowSynchronousIO = false;
});

// Add services to the container.
builder.Services.AddOpenApi();

// Thêm logging chi tiết
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Đọc cấu hình từ appsettings.json
var configuration = builder.Configuration;

// Đăng ký dịch vụ
builder.Services.ConfigureServices(configuration);

try
{
    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        await backend_manage.Data.SeedData.InitializeAsync(services);
    }


    // Health check Redis khi khởi động
    try
    {
        using var scope = app.Services.CreateScope();
        var redisService = scope.ServiceProvider.GetRequiredService<backend_manage.Services.Interfaces.IRedisService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        if (redisService.IsConnected)
        {
            logger.LogInformation("✅ Redis health check: Kết nối thành công");
        }
        else
        {
            logger.LogWarning("⚠️ Redis health check: Không kết nối được, đang sử dụng fallback");
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "❌ Redis health check thất bại");
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    // ✅ Gọi ConfigureMiddleware() (đã bao gồm kiểm tra token)
    app.ConfigureMiddleware();

    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}