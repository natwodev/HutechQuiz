using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs;

namespace frontend_manage.Pages.Exam;

public class ExamApi
{
    private readonly HttpClient _httpClient;
    
    public ExamApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SaveAnswerResponse?> SaveAnswerAsync(SaveAnswerDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/Student/save-answer", request);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔄 ExamApi: Response content: {responseContent}");
                var result = await response.Content.ReadFromJsonAsync<SaveAnswerResponse>();
                return result;
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<SubmitExamResponse?> SubmitExamAsync(SubmitExamRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/Student/submit-exam", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SubmitExamResponse>();
                return result;
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}