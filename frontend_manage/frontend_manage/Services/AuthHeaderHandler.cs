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
        Console.WriteLine($"🔄 AuthHeaderHandler: Request URL: {request.RequestUri}");
        
        if (!request.RequestUri?.AbsolutePath.Contains("api/auth/login", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
            Console.WriteLine($"🔄 AuthHeaderHandler: Token from localStorage: {(string.IsNullOrEmpty(token) ? "NULL" : "EXISTS")}");
            
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine($"🔄 AuthHeaderHandler: Added Bearer token to request");
            }
            else
            {
                Console.WriteLine($"❌ AuthHeaderHandler: No token found in localStorage");
            }
        }
        else
        {
            Console.WriteLine($"🔄 AuthHeaderHandler: Skipping token for login request");
        }

        return await base.SendAsync(request, cancellationToken);
    }
} 