using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using frontend_manage.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace frontend_manage.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "authToken";
    private const string StudentInfoKey = "studentInfo";
    public event Action? OnAuthStateChanged;

    public AuthService(HttpClient httpClient, NavigationManager navigationManager, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _navigationManager = navigationManager;
        _jsRuntime = jsRuntime;
    }

    public async Task<AuthResultDto> Login(string username, string password)
    {
        try
        {
            var loginModel = new LoginModelDto
            {
                UserName = username,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginModel);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                try
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    if (jsonDoc.RootElement.TryGetProperty("token", out var tokenElement) && tokenElement.ValueKind == JsonValueKind.String)
                    {
                        var token = tokenElement.GetString();
                        if (!string.IsNullOrEmpty(token))
                        {
                            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
                            OnAuthStateChanged?.Invoke();
                            return new AuthResultDto { IsSuccess = true, Token = token };
                        }
                    }
                    return new AuthResultDto { IsSuccess = false, ErrorMessage = "Không thể đọc token từ server" };
                }
                catch (JsonException)
                {
                    return new AuthResultDto { IsSuccess = false, ErrorMessage = "Phản hồi từ server không hợp lệ." };
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            try
            {
                var errorResult = JsonSerializer.Deserialize<AuthResultDto>(errorContent);
                return errorResult ?? new AuthResultDto { IsSuccess = false, ErrorMessage = "Đăng nhập thất bại" };
            }
            catch
            {
                return new AuthResultDto { IsSuccess = false, ErrorMessage = "Đăng nhập thất bại" };
            }
        }
        catch (Exception ex)
        {
            return new AuthResultDto { IsSuccess = false, ErrorMessage = $"Lỗi: {ex.Message}" };
        }
    }

    public async Task<AuthResultDto> LoginStudent(string studentCode, string password)
    {
        try
        {
            var loginModel = new LoginModelDto
            {
                UserName = studentCode,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("api/student/login", loginModel);
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
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
                        if (root.TryGetProperty("studentInfo", out var studentInfoElement))
                        {
                            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StudentInfoKey, studentInfoElement.GetRawText());
                            if (studentInfoElement.TryGetProperty("studentCode", out var studentCodeElement) && studentCodeElement.ValueKind == JsonValueKind.String)
                            {
                                var studentCodeValue = studentCodeElement.GetString();
                                if (!string.IsNullOrEmpty(studentCodeValue))
                                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "studentCode", studentCodeValue);
                            }
                        }
                        OnAuthStateChanged?.Invoke();
                        return new AuthResultDto { IsSuccess = true, Token = token };
                    }
                    // Nếu không có token, kiểm tra errorMessage
                    if (root.TryGetProperty("errorMessage", out var errorMsgElement) && errorMsgElement.ValueKind == JsonValueKind.String)
                    {
                        var errorMsg = errorMsgElement.GetString();
                        if (errorMsg == "Không tìm thấy sinh viên với mã này.")
                            errorMsg = "Không tìm thấy thông tin thí sinh, vui lòng liên hệ cán bộ coi thi";
                        return new AuthResultDto { IsSuccess = false, ErrorMessage = errorMsg };
                    }
                    return new AuthResultDto { IsSuccess = false, ErrorMessage = "Không thể đọc token từ server" };
                }
                catch (JsonException)
                {
                    return new AuthResultDto { IsSuccess = false, ErrorMessage = "Phản hồi từ server không hợp lệ." };
                }
            }
            // Trường hợp lỗi
            try
            {
                var errorResult = JsonSerializer.Deserialize<AuthResultDto>(responseContent);
                if (errorResult != null && errorResult.ErrorMessage == "Không tìm thấy sinh viên với mã này.")
                    errorResult.ErrorMessage = "Không tìm thấy thông tin thí sinh, vui lòng liên hệ cán bộ coi thi";
                return errorResult ?? new AuthResultDto { IsSuccess = false, ErrorMessage = "Đăng nhập thất bại" };
            }
            catch
            {
                return new AuthResultDto { IsSuccess = false, ErrorMessage = "Đăng nhập thất bại" };
            }
        }
        catch (Exception ex)
        {
            return new AuthResultDto { IsSuccess = false, ErrorMessage = $"Lỗi: {ex.Message}" };
        }
    }

    public async Task<StudentInfoDto?> GetStudentInfoAsync()
    {
        var studentInfoJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StudentInfoKey);
        if (string.IsNullOrEmpty(studentInfoJson)) return null;
        try
        {
            return JsonSerializer.Deserialize<StudentInfoDto>(studentInfoJson);
        }
        catch
        {
            return null;
        }
    }

    public async Task Logout()
    {
        var role = await GetUserRoleFromToken();
        var response = await _httpClient.PostAsync("api/auth/logout", null);

        if (response.IsSuccessStatusCode)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StudentInfoKey);

            OnAuthStateChanged?.Invoke();

            if (role == "Student")
            {
                _navigationManager.NavigateTo("/student-login");
            }
            else
            {
                _navigationManager.NavigateTo("/login");
            }
        }
        else
        {
            Console.WriteLine("Logout failed: " + response.ReasonPhrase);
        }
    }


    public async Task<bool> IsAuthenticated()
    {
        var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
        return !string.IsNullOrEmpty(token);
    }

    public async Task<string?> GetUserRoleFromToken()
    {
        var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            if (jwtToken == null)
            {
                return null;
            }

            var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role");
            return roleClaim?.Value;
        }
        catch
        {
            return null;
        }
    }

    public async Task InitializeAuthState()
    {
        // Không cần thêm token vào header nữa vì đã có AuthHeaderHandler
        OnAuthStateChanged?.Invoke();
    }

    public async Task<bool> IsAdmin()
    {
        var role = await GetUserRoleFromToken();
        return role == "Admin";
    }

    public async Task<bool> IsLecturer()
    {
        var role = await GetUserRoleFromToken();
        return role == "Lecturer";
    }
}