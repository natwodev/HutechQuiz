using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs;
using FrontEnd.DTOs;

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
}