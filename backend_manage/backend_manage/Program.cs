using backend_manage.Extensions;
using OfficeOpenXml;
using Serilog;
using Microsoft.EntityFrameworkCore;
using backend_manage.Messages.RabbitMQ;
using backend_manage.Services.AuthService.Helpers;



var builder = WebApplication.CreateBuilder(args);

// Cấu hình Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

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
        
        // Preload approved papers vào Redis cache
        try
        {
            var examPaperHelper = services.GetRequiredService<ExamPaperHelper>();
            await examPaperHelper.PreloadAllApprovedPapersAsync();
            Log.Information("Đã preload approved papers vào Redis cache thành công");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Không thể preload approved papers vào Redis cache, hệ thống sẽ chạy bình thường");
            // Không throw exception để app vẫn chạy được
        }
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    // ✅ Gọi ConfigureMiddleware() (đã bao gồm kiểm tra token)
    app.ConfigureMiddleware();

    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

    // Start RabbitMQ consumer với error handling
    try
    {
        var rabbitMqConsumer = app.Services.GetRequiredService<IRabbitMqConsumer>();
        rabbitMqConsumer.StartConsuming();
        Log.Information("RabbitMQ consumer đã được khởi động thành công");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Không thể khởi động RabbitMQ consumer, hệ thống sẽ chạy không có message queue");
        // Không throw exception để app vẫn chạy được
    }

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