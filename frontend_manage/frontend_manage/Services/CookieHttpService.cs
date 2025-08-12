using Microsoft.JSInterop;
using System.Text;
using System.Text.Json;

namespace frontend_manage.Services;

public class CookieHttpService
{
    private readonly IJSRuntime _jsRuntime;

    public CookieHttpService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        var response = await _jsRuntime.InvokeAsync<string>("fetchWithCredentials", url, "GET", null);
        if (string.IsNullOrEmpty(response))
            return default(T);

        return JsonSerializer.Deserialize<T>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public async Task<T?> PostAsync<T>(string url, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var response = await _jsRuntime.InvokeAsync<string>("fetchWithCredentials", url, "POST", json);
        if (string.IsNullOrEmpty(response))
            return default(T);

        return JsonSerializer.Deserialize<T>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public async Task<string> GetStringAsync(string url)
    {
        return await _jsRuntime.InvokeAsync<string>("fetchWithCredentials", url, "GET", null);
    }

    public async Task<string> PostStringAsync(string url, object data)
    {
        var json = JsonSerializer.Serialize(data);
        return await _jsRuntime.InvokeAsync<string>("fetchWithCredentials", url, "POST", json);
    }
}
