using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;

namespace frontend_manage.Services;

/// <summary>
/// Service for making API calls that properly handle authentication.
/// Originally designed for cookie-based auth, now modified to use JWT tokens since
/// we can't modify the backend CORS policy to allow credentials.
/// </summary>
public class CookieHttpService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly string _baseUrl;
    private readonly AuthService _authService;
    
    public CookieHttpService(IJSRuntime jsRuntime, AuthService authService = null)
    {
        _jsRuntime = jsRuntime;
        _baseUrl = "http://localhost:5163/"; // Same as in Program.cs
        _authService = authService;
    }
    
    /// <summary>
    /// Makes a GET request with appropriate authentication
    /// </summary>
    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            var url = $"{_baseUrl}{endpoint.TrimStart('/')}";
            
            // Get token and auth type
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            var authType = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authType");
            
            // Log for debugging
            Console.WriteLine($"[CookieHttpService] GetAsync {url} - AuthType: {authType ?? "null"}");
            
            // First try using HttpClient with JWT token (AuthHeaderHandler will add the token)
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_baseUrl);
            
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine("[CookieHttpService] Using token authentication");
            }
            
            try
            {
                var response = await httpClient.GetFromJsonAsync<T>(endpoint);
                Console.WriteLine("[CookieHttpService] HttpClient request succeeded");
                return response;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[CookieHttpService] HttpClient failed: {ex.Message}, trying fetchWithCredentials");
                
                // If that fails, try with the fetchWithCredentials function
                var response = await _jsRuntime.InvokeAsync<string>("fetchWithCredentials", url, "GET");
                
                // Check for error response
                if (!string.IsNullOrEmpty(response) && response.Contains("\"error\""))
                {
                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<Dictionary<string, object>>(response);
                        if (errorObj != null && errorObj.ContainsKey("error"))
                        {
                            if (errorObj["error"].ToString() == "Unauthorized")
                            {
                                throw new UnauthorizedAccessException("Authentication required");
                            }
                            
                            throw new HttpRequestException($"Request failed: {errorObj["message"]}");
                        }
                    }
                    catch (JsonException)
                    {
                        // Continue with normal processing if not valid error JSON
                    }
                }
                
                if (string.IsNullOrEmpty(response))
                {
                    return default;
                }
                
                return JsonSerializer.Deserialize<T>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("[CookieHttpService] Unauthorized access exception");
            throw; // Let the caller handle authentication errors
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CookieHttpService] Error in GetAsync: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Makes a POST request with appropriate authentication
    /// </summary>
    public async Task<T?> PostAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            var url = $"{_baseUrl}{endpoint.TrimStart('/')}";
            
            // First try using HttpClient with JWT token
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_baseUrl);
            
            // Get token from localStorage
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            
            try
            {
                var response = await httpClient.PostAsJsonAsync(endpoint, data);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (HttpRequestException)
            {
                // If that fails, try with the fetchWithCredentials function
                var jsonContent = data != null ? JsonSerializer.Serialize(data) : null;
                
                var response = await _jsRuntime.InvokeAsync<string>(
                    "fetchWithCredentials", 
                    url, 
                    "POST", 
                    jsonContent);
                
                if (string.IsNullOrEmpty(response))
                {
                    return default;
                }
                
                return JsonSerializer.Deserialize<T>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in PostAsync: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Makes a PUT request using JavaScript fetch to ensure cookies are sent
    /// </summary>
    public async Task<T?> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            var url = $"{_baseUrl}{endpoint.TrimStart('/')}";
            var jsonContent = JsonSerializer.Serialize(data);
            
            var response = await _jsRuntime.InvokeAsync<string>(
                "fetchWithCredentials", 
                url, 
                "PUT", 
                jsonContent);
            
            if (string.IsNullOrEmpty(response))
            {
                return default;
            }
            
            return JsonSerializer.Deserialize<T>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in PutAsync: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Makes a DELETE request using JavaScript fetch to ensure cookies are sent
    /// </summary>
    public async Task<T?> DeleteAsync<T>(string endpoint)
    {
        try
        {
            var url = $"{_baseUrl}{endpoint.TrimStart('/')}";
            var response = await _jsRuntime.InvokeAsync<string>("fetchWithCredentials", url, "DELETE");
            
            if (string.IsNullOrEmpty(response))
            {
                return default;
            }
            
            return JsonSerializer.Deserialize<T>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DeleteAsync: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Makes a PATCH request using JavaScript fetch to ensure cookies are sent
    /// </summary>
    public async Task<T?> PatchAsync<T>(string endpoint, object data)
    {
        try
        {
            var url = $"{_baseUrl}{endpoint.TrimStart('/')}";
            var jsonContent = JsonSerializer.Serialize(data);
            
            var response = await _jsRuntime.InvokeAsync<string>(
                "fetchWithCredentials", 
                url, 
                "PATCH", 
                jsonContent);
            
            if (string.IsNullOrEmpty(response))
            {
                return default;
            }
            
            return JsonSerializer.Deserialize<T>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in PatchAsync: {ex.Message}");
            throw;
        }
    }
}
