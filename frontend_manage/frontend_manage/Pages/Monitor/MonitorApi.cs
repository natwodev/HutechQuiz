using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using frontend_manage.DTOs;
using frontend_manage.DTOs.AcademicAffairs;

namespace frontend_manage.Pages.Monitor;

public class MonitorApi
{
    private readonly HttpClient _httpClient;
    
    public MonitorApi(HttpClient httpClient)
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

    public async Task<List<ExamRoomLecturerAssignmentDto>?> GetMyAssignmentsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ExamRoomLecturerAssignmentDto>>("/api/ExamRoomLecturerAssignment/my-assignments");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting lecturer assignments: {ex.Message}");
            return null;
        }
    }
}
