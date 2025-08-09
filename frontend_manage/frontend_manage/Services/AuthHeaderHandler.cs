using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace frontend_manage.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "authToken";

    public AuthHeaderHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Bỏ qua các API authentication
        if (request.RequestUri?.AbsolutePath.Contains("api/auth/", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            return await base.SendAsync(request, cancellationToken);
        }
        
        // Kiểm tra loại authentication
        var authType = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authType");
        
        if (authType == "cookie")
        {
            // Cookie authentication - không cần thêm header, cookie sẽ tự động gửi
        }
        else
        {
            // JWT authentication - thêm Bearer token
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
            
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
} 