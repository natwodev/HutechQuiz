using System.Net.Http.Json;

namespace frontend_manage.Services.Admin;

public class SystemService
{
    private readonly HttpClient _httpClient;
    
    public SystemService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> SendNotificationAsync(string message, DateTime examTime)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/Notification/send-exam-reminder", new 
            { 
                Message = message, 
                ExamTime = examTime 
            });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<QueueStatusDto?> CheckQueueStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/QueueStatus/check");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<QueueStatusResponse>();
                return new QueueStatusDto
                {
                    Message = result?.Message ?? "Đã kiểm tra",
                    IsHealthy = true
                };
            }
            return new QueueStatusDto { IsHealthy = false, Message = "Lỗi khi kiểm tra" };
        }
        catch
        {
            return new QueueStatusDto { IsHealthy = false, Message = "Lỗi kết nối" };
        }
    }
}

public class QueueStatusDto
{
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class QueueStatusResponse
{
    public string Message { get; set; } = string.Empty;
}


