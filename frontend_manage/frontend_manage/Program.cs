
using frontend_manage;
using frontend_manage.Pages.Admin;
using frontend_manage.Pages.StudentLogin;
using frontend_manage.Pages.Exam;
using frontend_manage.Pages.Monitor;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Microsoft.Extensions.Http;
using Microsoft.JSInterop;

using frontend_manage.Services;
using frontend_manage.Services.AcademicAffairs;


var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add MudBlazor services
builder.Services.AddMudServices();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CookieHttpService>();
builder.Services.AddScoped<InfoApi>();
builder.Services.AddScoped<ExamApi>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddScoped<Api>();

// Academic Affairs Services
builder.Services.AddScoped<AcademicYearService>();
builder.Services.AddScoped<SemesterService>();
builder.Services.AddScoped<ExamBatchService>();
builder.Services.AddScoped<ExamBatchDetailService>();
builder.Services.AddScoped<ExamSessionService>();
builder.Services.AddScoped<ExamSessionDepartmentService>();
builder.Services.AddScoped<ExamSessionSubjectService>();

// Đăng ký AuthHeaderHandler
builder.Services.AddScoped<AuthHeaderHandler>();

// Cấu hình HttpClient với AuthHeaderHandler cho Blazor WebAssembly
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri("http://localhost:5163/");
    // Đảm bảo gửi credentials (cookies) với mọi request
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).AddHttpMessageHandler<AuthHeaderHandler>();

// Đăng ký HttpClient mặc định
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"));



var app = builder.Build();

// Khởi tạo trạng thái xác thực khi ứng dụng khởi động
var authService = app.Services.GetRequiredService<AuthService>();
await authService.InitializeAuthState();

await app.RunAsync();