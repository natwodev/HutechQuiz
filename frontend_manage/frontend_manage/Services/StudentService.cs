using System.Net.Http.Json;
using frontend_manage.DTOs;
using System.Linq;

namespace frontend_manage.Services;

public class StudentService
{
    private readonly HttpClient _httpClient;
    public StudentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<StudentInfoDto?> GetStudentProfileAsync()
    {
        return await _httpClient.GetFromJsonAsync<StudentInfoDto>("api/student/profile");
    }

    public async Task<List<StudentExamSessionDto>?> GetStudentExamSessionsAsync()
    {
        var allExamSessions = await _httpClient.GetFromJsonAsync<List<StudentExamSessionDto>>("api/Student/exam-sessions");
        
        return allExamSessions ?? new List<StudentExamSessionDto>();
    }

    public async Task<StartExamResponseDto?> StartExamAsync(int studentExamSessionId)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(studentExamSessionId.ToString()), "studentExamSessionId");
        var response = await _httpClient.PostAsync("api/Student/start-exam", form);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<StartExamResponseDto>();
        }
        return null;
    }

    public async Task<SaveAnswerResponse?> SaveAnswerAsync(SaveAnswerDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/Student/save-answer", request);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
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
