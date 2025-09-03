
using frontend_manage;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Http;


var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add application services
builder.Services.AddApplicationServices();

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