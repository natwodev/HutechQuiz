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
    // Removed cookie-based auth support
    public event Action? OnAuthStateChanged;

    public AuthService(HttpClient httpClient, NavigationManager navigationManager, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _navigationManager = navigationManager;
        _jsRuntime = jsRuntime;
        
        // Đảm bảo BaseAddress được set
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri("http://localhost:5163/");
        }
    }

    // JWT Authentication (cho các trường hợp khác)
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

    // Cookie-based auth removed

    public async Task<AuthResultDto> LoginStudent(string studentCode1, string studentCode2)
    {
        try
        {
            var loginRequest = new StudentLoginRequestDto
            {
                StudentCode1 = studentCode1,
                StudentCode2 = studentCode2
            };

            var response = await _httpClient.PostAsJsonAsync("api/student/login", loginRequest);
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
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authType", "jwt");
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

    // Lecturer JWT login
    public async Task<LecturerAuthResultDto> LoginLecturer(string lecturerCode1, string lecturerCode2)
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
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authType", "jwt");
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "userType", "lecturer");
                        OnAuthStateChanged?.Invoke();
                        return new LecturerAuthResultDto { IsSuccess = true, Token = token, Role = "Lecturer" };
                    }

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

    

    public async Task Logout()
    {
        // Cookie-based logout flow removed

        var role = await GetUserRoleFromToken();
        
        // Xóa tất cả thông tin xác thực khỏi localStorage
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StudentInfoKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authType");

        // Thông báo thay đổi trạng thái xác thực
        OnAuthStateChanged?.Invoke();

        // Chuyển hướng dựa trên vai trò
        if (role == "Student")
        {
            _navigationManager.NavigateTo("/student-login");
        }
        else
        {
            _navigationManager.NavigateTo("/");
        }
    }

    public async Task<bool> IsAuthenticated()
    {
        // Cookie-based auth removed
        var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
        return !string.IsNullOrEmpty(token);
    }

    public async Task<string?> GetUserRoleFromToken()
    {
        // Cookie-based role parsing removed

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
        // Cookie-based initialization removed; rely on JWT/localStorage only
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

    public async Task<bool> IsAcademicAffairs()
    {
        var role = await GetUserRoleFromToken();
        return role == "AcademicAffairs";
    }

    public async Task<bool> HasRequiredRole()
    {
        var role = await GetUserRoleFromToken();
        return role == "Admin" || role == "AcademicAffairs";
    }
}
