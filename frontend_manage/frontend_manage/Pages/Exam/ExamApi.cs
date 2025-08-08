using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs;
using Microsoft.JSInterop;

namespace frontend_manage.Pages.Exam;

public class ExamApi
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    
    public ExamApi(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public async Task<SaveAnswerResponse?> SaveAnswerAsync(SaveAnswerDto request)
    {
        try
        {
            Console.WriteLine($"🔄 ExamApi: Đang gửi request đến /api/Student/save-answer");
            Console.WriteLine($"🔄 ExamApi: Request data: {System.Text.Json.JsonSerializer.Serialize(request)}");
            
            var response = await _httpClient.PostAsJsonAsync("/api/Student/save-answer", request);
            
            Console.WriteLine($"🔄 ExamApi: Response status: {response.StatusCode}");
            Console.WriteLine($"🔄 ExamApi: Response headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔄 ExamApi: Response content: {responseContent}");
                
                var result = await response.Content.ReadFromJsonAsync<SaveAnswerResponse>();
                Console.WriteLine($"🔄 ExamApi: Parsed response: {System.Text.Json.JsonSerializer.Serialize(result)}");
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ ExamApi: Error response: {errorContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ExamApi: Exception: {ex.Message}");
            Console.WriteLine($"❌ ExamApi: Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    public async Task<string?> GetStudentAnswersAsync(int studentExamSessionId)
    {
        try
        {
            Console.WriteLine($"🔄 ExamApi: Đang lấy đáp án đã lưu cho session {studentExamSessionId}");
            
            var response = await _httpClient.GetAsync($"/api/Student/answers/{studentExamSessionId}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✅ ExamApi: Đã lấy đáp án: {result}");
                return result;
            }
            else
            {
                Console.WriteLine($"❌ ExamApi: Lỗi khi lấy đáp án - Status: {response.StatusCode}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ExamApi: Exception khi lấy đáp án: {ex.Message}");
            return null;
        }
    }
}