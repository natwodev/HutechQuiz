using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using frontend_manage.DTOs;
using frontend_manage.DTOs.AcademicAffairs;

namespace frontend_manage.Services;

public class MonitorService
{
    private readonly HttpClient _httpClient;
    
    public MonitorService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }


    public async Task<LecturerAuthResultDto?> LoginLecturerAsync(string lecturerCode1, string lecturerCode2)
    {
        try
        {
            var loginModel = new LecturerLoginDto
            {
                LecturerCode1 = lecturerCode1,
                LecturerCode2 = lecturerCode2
            };

            var response = await _httpClient.PostAsJsonAsync("/api/auth/lecturer-login", loginModel);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    using var jsonDoc = JsonDocument.Parse(responseContent);
                    var root = jsonDoc.RootElement;
                    var token = root.TryGetProperty("token", out var tokenElement) && tokenElement.ValueKind == JsonValueKind.String
                        ? tokenElement.GetString() : null;
                    
                    if (!string.IsNullOrEmpty(token))
                    {
                        return new LecturerAuthResultDto { IsSuccess = true, Token = token, Role = "Lecturer" };
                    }
                    
                    // Nếu không có token, kiểm tra errorMessage
                    if (root.TryGetProperty("errorMessage", out var errorMsgElement) && errorMsgElement.ValueKind == JsonValueKind.String)
                    {
                        var errorMsg = errorMsgElement.GetString();
                        return new LecturerAuthResultDto { IsSuccess = false, ErrorMessage = errorMsg };
                    }
                    
                    return new LecturerAuthResultDto { IsSuccess = false, ErrorMessage = "Không thể đọc token từ server" };
                }
                catch (JsonException)
                {
                    return new LecturerAuthResultDto { IsSuccess = false, ErrorMessage = "Phản hồi từ server không hợp lệ." };
                }
            }
            
            // Trường hợp lỗi
            try
            {
                var errorResult = JsonSerializer.Deserialize<LecturerAuthResultDto>(responseContent);
                return errorResult ?? new LecturerAuthResultDto { IsSuccess = false, ErrorMessage = "Đăng nhập thất bại" };
            }
            catch
            {
                return new LecturerAuthResultDto { IsSuccess = false, ErrorMessage = "Đăng nhập thất bại" };
            }
        }
        catch (Exception ex)
        {
            return new LecturerAuthResultDto { IsSuccess = false, ErrorMessage = $"Lỗi: {ex.Message}" };
        }
    }
    
    public async Task<LecturerDto?> GetLecturerInfoAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<LecturerDto>("/api/lecturer/profile");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting lecturer info: {ex.Message}");
            return null;
        }
    }

    public async Task<List<SubjectExamRoomStatusDto>?> GetMyAssignmentsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<SubjectExamRoomStatusDto>>("/api/ExamSessionSubject/lecturer/subject-exam-room-status");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting lecturer assignments: {ex.Message}");
            return null;
        }
    }

    public async Task<StudentListResponse?> GetStudentsByExamRoomAsync(int examSessionSubjectId)
    {
        try
        {
            var result = await _httpClient
                .GetFromJsonAsync<StudentListResponse>(
                    $"/api/ExamSessionSubject/{examSessionSubjectId}/with-students"
                );

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting students by exam room: {ex.Message}");
            return null;
        }
    }


    public async Task<string> AddExtraMinutesAsync(string studentCode, int studentExamSessionId, int extraMinutes, string? reasonForExtra)
    {
        try
        {
            // Tạo request object theo đúng cấu trúc backend
            var request = new AddExtraMinutesRequest
            {
                StudentCode = studentCode,
                StudentExamSessionId = studentExamSessionId,
                ExtraMinutes = extraMinutes,
                ReasonForExtra = reasonForExtra
            };


            var response = await _httpClient.PostAsJsonAsync("/api/Student/extra-minutes", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (result != null && result.TryGetValue("message", out var message))
                    return message;
                return "Thêm phút thành công.";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"HTTP {response.StatusCode}: {errorContent}");
                
                try
                {
                    var errorResult = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    if (errorResult != null && errorResult.TryGetValue("message", out var errorMessage))
                        return $"Lỗi {response.StatusCode}: {errorMessage}";
                }
                catch
                {
                    // Nếu không parse được JSON, trả về raw content
                }
                
                return $"Lỗi {response.StatusCode}: {errorContent}";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding extra minutes: {ex.Message}");
            return $"Lỗi: {ex.Message}";
        }
    }

    public class AddExtraMinutesRequest
    {
        public string StudentCode { get; set; } = string.Empty;
        public int StudentExamSessionId { get; set; }
        public int ExtraMinutes { get; set; }
        public string? ReasonForExtra { get; set; }
    }

    public async Task<string> ActiveLoginAsync(string studentCode, bool isLogin)
    {
        try
        {
            var request = new ActiveLoginRequest
            {
                StudentCode = studentCode,
                IsLogin = isLogin
            };

            var response = await _httpClient.PostAsJsonAsync("/api/student/active-login", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (result != null && result.TryGetValue("message", out var message))
                    return message;
                return "Cập nhật trạng thái đăng nhập thành công.";
            }
            else
            {
                var errorResult = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (errorResult != null && errorResult.TryGetValue("message", out var errorMessage))
                    return errorMessage;
                return "Lỗi không xác định từ server.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating login status: {ex.Message}");
            return $"Lỗi: {ex.Message}";
        }
    }

    
    
    public class ActiveLoginRequest
    {
        public string StudentCode { get; set; } = string.Empty;
        public bool IsLogin { get; set; }
    }

    public async Task<string> UpdateIsActiveAsync(int examSessionSubjectId, bool isActive)
    {
        try
        {
            var request = new ActiveExamSessionSubject
            {
                examSessionSubjectId = examSessionSubjectId,
                isActive = isActive
            };

            var response = await _httpClient.PostAsJsonAsync("/api/ExamSessionSubject/is-active", request);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Success response: {responseContent}");
                
                // Try to parse as simple message object first
                try
                {
                    var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                    if (result != null && result.TryGetValue("message", out var message))
                        return message.ToString() ?? "Cập nhật trạng thái hoạt động thành công";
                }
                catch
                {
                    // If parsing fails, try to get the raw content
                    return responseContent;
                }
                
                return "Cập nhật trạng thái hoạt động thành công";
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error response: {responseContent}");
                
                // Try to parse error response
                try
                {
                    var errorResult = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                    if (errorResult != null && errorResult.TryGetValue("message", out var errorMessage))
                        return errorMessage.ToString() ?? $"Lỗi {response.StatusCode}";
                }
                catch
                {
                    // If parsing fails, return raw content
                }
                
                return $"Lỗi {response.StatusCode}: {responseContent}";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating isActive status: {ex.Message}");
            return $"Lỗi: {ex.Message}";
        }
    }

    public class ActiveExamSessionSubject
    {
        public int examSessionSubjectId { get; set; }
        public bool isActive { get; set; }
    }
}
