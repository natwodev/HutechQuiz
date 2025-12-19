using System.Net.Http.Json;
using frontend_manage.DTOs;
using Microsoft.Extensions.Configuration;

namespace frontend_manage.Services;

public class StudentActivityService
{
    private readonly HttpClient _httpClient;

    public StudentActivityService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<StudentActivityDto>> GetActivitiesByExamSessionAsync(int examSessionSubjectId, DateTime? fromTime = null, DateTime? toTime = null)
    {
        try
        {
            // Sử dụng relative URL không có leading slash
            var url = $"api/StudentActivity/by-exam-session/{examSessionSubjectId}";
            if (fromTime.HasValue)
            {
                url += $"?fromTime={fromTime.Value:yyyy-MM-ddTHH:mm:ss}";
            }
            if (toTime.HasValue)
            {
                url += $"{(fromTime.HasValue ? "&" : "?")}toTime={toTime.Value:yyyy-MM-ddTHH:mm:ss}";
            }

            Console.WriteLine($"[StudentActivityService] Getting activities from: {url}");
            Console.WriteLine($"[StudentActivityService] BaseAddress: {_httpClient.BaseAddress}");
            Console.WriteLine($"[StudentActivityService] Full URL will be: {_httpClient.BaseAddress}{url}");

            var response = await _httpClient.GetAsync(url);
            
            Console.WriteLine($"[StudentActivityService] Response Status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[StudentActivityService] Error response: {errorContent}");
                throw new HttpRequestException($"Request failed with status {response.StatusCode}: {errorContent}");
            }
            
            var activities = await response.Content.ReadFromJsonAsync<List<StudentActivityDto>>();
            return activities ?? new List<StudentActivityDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StudentActivityService] Error getting activities: {ex.Message}");
            Console.WriteLine($"[StudentActivityService] StackTrace: {ex.StackTrace}");
            return new List<StudentActivityDto>();
        }
    }

    public async Task<List<StudentActivityDto>> GetActivitiesByStudentCodeAsync(string studentCode, int? examSessionSubjectId = null)
    {
        try
        {
            // Sử dụng relative URL không có leading slash
            var url = $"api/StudentActivity/by-student/{studentCode}";
            if (examSessionSubjectId.HasValue)
            {
                url += $"?examSessionSubjectId={examSessionSubjectId.Value}";
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var activities = await response.Content.ReadFromJsonAsync<List<StudentActivityDto>>();
            return activities ?? new List<StudentActivityDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting activities by student: {ex.Message}");
            return new List<StudentActivityDto>();
        }
    }

    public async Task<bool> RecordActivityAsync(RecordActivityDto dto)
    {
        try
        {
            // Sử dụng relative URL không có leading slash để combine với BaseAddress
            var url = "api/StudentActivity/record";
            Console.WriteLine($"[StudentActivityService] Calling API: {url}");
            Console.WriteLine($"[StudentActivityService] BaseAddress: {_httpClient.BaseAddress}");
            Console.WriteLine($"[StudentActivityService] Full URL will be: {_httpClient.BaseAddress}{url}");
            Console.WriteLine($"[StudentActivityService] DTO: StudentExamSessionId={dto.StudentExamSessionId}, StudentCode={dto.StudentCode}, ActivityType={dto.ActivityType}");
            
            var response = await _httpClient.PostAsJsonAsync(url, dto);
            
            Console.WriteLine($"[StudentActivityService] Response Status: {response.StatusCode}");
            Console.WriteLine($"[StudentActivityService] Request URI: {response.RequestMessage?.RequestUri}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[StudentActivityService] Success response: {content}");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[StudentActivityService] Error recording activity: {response.StatusCode} - {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StudentActivityService] Exception recording activity: {ex.Message}");
            Console.WriteLine($"[StudentActivityService] StackTrace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<int> GetMyViolationCountAsync(int examSessionSubjectId)
    {
        try
        {
            var url = $"api/StudentActivity/my-violation-count?examSessionSubjectId={examSessionSubjectId}";
            Console.WriteLine($"[StudentActivityService] Getting violation count from: {url}");
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ViolationCountResponse>();
                Console.WriteLine($"[StudentActivityService] Violation count: {result?.ViolationCount ?? 0}");
                return result?.ViolationCount ?? 0;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[StudentActivityService] Error getting violation count: {response.StatusCode} - {errorContent}");
                return 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StudentActivityService] Exception getting violation count: {ex.Message}");
            Console.WriteLine($"[StudentActivityService] StackTrace: {ex.StackTrace}");
            return 0;
        }
    }

    private class ViolationCountResponse
    {
        public string StudentCode { get; set; } = string.Empty;
        public int ViolationCount { get; set; }
        public int MaxViolations { get; set; }
        public int TotalActivities { get; set; }
    }
}

