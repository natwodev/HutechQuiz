using System.Net.Http.Json;
using frontend_manage.DTOs;

namespace frontend_manage.Pages.StudentLogin;

public class InfoApi
{
    private readonly HttpClient _httpClient;
    public InfoApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<StudentInfoDto?> GetStudentProfileAsync()
    {
        return await _httpClient.GetFromJsonAsync<StudentInfoDto>("api/student/profile");
    }

    public async Task<List<ExamSessionDto>?> GetStudentExamSessionsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<ExamSessionDto>>("api/Student/exam-sessions");
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
}