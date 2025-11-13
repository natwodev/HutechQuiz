
using frontend_manage;
using frontend_manage.Pages.StudentLogin;
using frontend_manage.Pages.Exam;
using frontend_manage.Pages.Monitor;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Microsoft.Extensions.Http;
using Microsoft.JSInterop;

using frontend_manage.Services;
using frontend_manage.Services.ExamManager;
using frontend_manage.Services.Admin; 


var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add MudBlazor services
builder.Services.AddMudServices();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<StudentService>();

builder.Services.AddScoped<MonitorService>();
builder.Services.AddSingleton<NotificationService>();

// Exam Manager Services
builder.Services.AddScoped<ExamManagerService>();

// Admin Services
builder.Services.AddScoped<AdminAcademicYearService>();
builder.Services.AddScoped<AdminSemesterService>();
builder.Services.AddScoped<AdminExamBatchService>();
builder.Services.AddScoped<AdminExamBatchDetailService>();
builder.Services.AddScoped<AdminExamSessionService>();
builder.Services.AddScoped<AdminExamSessionSubjectService>();
builder.Services.AddScoped<ExamRoomService>();
builder.Services.AddScoped<LecturerService>();
builder.Services.AddScoped<AdminStudentService>();
builder.Services.AddScoped<SystemService>();

// Removed MathJax service (migrated to KaTeX)
// Đăng ký KaTeX service
builder.Services.AddScoped<IKaTeXService, KaTeXService>();
// Mock exam service
builder.Services.AddSingleton<ExamMockService>();

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