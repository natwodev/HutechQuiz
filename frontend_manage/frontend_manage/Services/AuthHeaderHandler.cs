using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.JSInterop;
using System.Diagnostics;

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
        Debug.WriteLine($"[AuthHeaderHandler] Called for: {request.RequestUri}");
        
        // Bỏ qua các API authentication
        if (request.RequestUri?.AbsolutePath.Contains("api/auth/", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            Debug.WriteLine("[AuthHeaderHandler] Skipping auth header for auth API");
            return await base.SendAsync(request, cancellationToken);
        }

        try
        {
            // Always use JWT from localStorage; cookie-based auth removed
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
            Console.WriteLine($"[AuthHeaderHandler] JWT Token: {token ?? "null"}");
            Debug.WriteLine($"[AuthHeaderHandler] JWT Token: {token ?? "null"}");

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine($"[AuthHeaderHandler] Added Bearer token from JWT auth");
                Debug.WriteLine($"[AuthHeaderHandler] Added Bearer token from JWT auth");
            }
            else
            {
                Console.WriteLine($"[AuthHeaderHandler] No token found in localStorage!");
            }
            
            // Log final authorization header
            var authHeader = request.Headers.Authorization?.ToString();
            Console.WriteLine($"[AuthHeaderHandler] Final Authorization Header: {authHeader ?? "None"}");
            Debug.WriteLine($"[AuthHeaderHandler] Final Authorization Header: {authHeader ?? "None"}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AuthHeaderHandler] Error: {ex.Message}");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}