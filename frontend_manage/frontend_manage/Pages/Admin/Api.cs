using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs;

namespace frontend_manage.Pages.Admin;

public class Api
{
    private readonly HttpClient _httpClient;
    
    public Api(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ExamSessionSubjectRoomDto>?> GetExamSessionSubjectsWithRoomsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<ExamSessionSubjectRoomDto>>("/api/ExamSessionSubject/with-rooms");
    }

    public async Task<List<StudentExamRoomStatusDto>?> GetStudentsByExamRoomAsync(int examRoomId, int examSessionSubjectId)
    {
        var result = await _httpClient.GetFromJsonAsync<StudentListResponse>($"/api/Student/by-exam-room?examRoomId={examRoomId}&examSessionSubjectId={examSessionSubjectId}");
        return result?.Students;
    }
    public async Task<string> ActiveLoginAsync(string studentCode, bool isLogin)
    {
        var request = new ActiveLoginRequest
        {
            StudentCode = studentCode,
            IsLogin = isLogin
        };

        var response = await _httpClient.PostAsJsonAsync("/api/student/active-login", request);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        if (result != null && result.TryGetValue("message", out var message))
            return message;

        return "Lỗi không xác định từ server.";
    }

    public class ActiveLoginRequest
    {
        public string StudentCode { get; set; } = string.Empty;
        public bool IsLogin { get; set; }
    }


}