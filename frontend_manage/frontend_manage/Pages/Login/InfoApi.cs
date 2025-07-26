using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FrontEnd.DTOs;

namespace frontend_manage.Pages.Login;

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

    public async Task<ShuffledExamPaperDto?> StartExamAsync(int examSessionSubjectId)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(examSessionSubjectId.ToString()), "examSessionSubjectId");
        var response = await _httpClient.PostAsync("api/Student/start-exam", form);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ShuffledExamPaperDto>();
        }
        return null;
    }
}