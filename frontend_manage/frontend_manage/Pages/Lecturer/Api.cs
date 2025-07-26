using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs;

namespace frontend_manage.Pages.Lecturer;

public class Api
{
    private readonly HttpClient _httpClient;
    
    public Api(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Lấy danh sách phòng thi được phân công cho giảng viên
    public async Task<List<LecturerExamRoomDto>?> GetMyExamRoomsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<LecturerExamRoomDto>>("/api/ExamRoomLecturerAssignment/my-assignments");
    }

    // Lấy danh sách sinh viên trong phòng thi
    public async Task<List<StudentExamRoomStatusDto>?> GetStudentsByExamRoomAsync(int examRoomId, int examSessionSubjectId)
    {
        var result = await _httpClient.GetFromJsonAsync<StudentListResponse>($"/api/Student/by-exam-room?examRoomId={examRoomId}&examSessionSubjectId={examSessionSubjectId}");
        return result?.Students;
    }

    // Gửi thông báo cho sinh viên trong phòng thi
    public async Task<string> SendNotificationToRoomAsync(int examRoomId, string message)
    {
        var request = new NotificationRequest
        {
            ExamRoomId = examRoomId,
            Message = message
        };

        var response = await _httpClient.PostAsJsonAsync("/api/Notification/send-to-room", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return result?.GetValueOrDefault("message", "Thông báo đã được gửi") ?? "Thông báo đã được gửi";
        }

        return "Lỗi khi gửi thông báo";
    }

    // Logout sinh viên khỏi phòng thi
    public async Task<string> LogoutStudentAsync(string studentCode, int examRoomId, string reason = "Giảng viên yêu cầu")
    {
        var request = new LogoutStudentRequest
        {
            StudentCode = studentCode,
            ExamRoomId = examRoomId,
            Reason = reason
        };

        var response = await _httpClient.PostAsJsonAsync("/api/Student/logout", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return result?.GetValueOrDefault("message", "Đã logout sinh viên") ?? "Đã logout sinh viên";
        }

        return "Lỗi khi logout sinh viên";
    }

    // Logout tất cả sinh viên trong phòng thi
    public async Task<string> LogoutAllStudentsInRoomAsync(int examRoomId, string reason = "Giảng viên yêu cầu")
    {
        var request = new LogoutAllStudentsRequest
        {
            ExamRoomId = examRoomId,
            Reason = reason
        };

        var response = await _httpClient.PostAsJsonAsync("/api/Student/logout-all", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return result?.GetValueOrDefault("message", "Đã logout tất cả sinh viên") ?? "Đã logout tất cả sinh viên";
        }

        return "Lỗi khi logout sinh viên";
    }

    public class NotificationRequest
    {
        public int ExamRoomId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class LogoutStudentRequest
    {
        public string StudentCode { get; set; } = string.Empty;
        public int ExamRoomId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class LogoutAllStudentsRequest
    {
        public int ExamRoomId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
} 