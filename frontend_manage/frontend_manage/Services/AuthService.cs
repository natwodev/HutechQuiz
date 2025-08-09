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
    private const string CookieAuthKey = "cookieAuth";
    public event Action? OnAuthStateChanged;

    public AuthService(HttpClient httpClient, NavigationManager navigationManager, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _navigationManager = navigationManager;
        _jsRuntime = jsRuntime;
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

    // Cookie Authentication (cho admin)
    public async Task<AuthResultDto> LoginWithCookie(string username, string password)
    {
        try
        {
            var loginModel = new LoginModelDto
            {
                UserName = username,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("api/auth/login-cookie", loginModel);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                try
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var root = jsonDoc.RootElement;
                    
                    // Lưu thông tin user vào localStorage
                    if (root.TryGetProperty("user", out var userElement))
                    {
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CookieAuthKey, userElement.GetRawText());
                    }
                    
                    // Đánh dấu đã đăng nhập bằng cookie
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authType", "cookie");
                    
                    OnAuthStateChanged?.Invoke();
                    return new AuthResultDto { IsSuccess = true, Token = null }; // Cookie không cần token
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

    // Kiểm tra trạng thái đăng nhập cookie
    public async Task<AuthResultDto> CheckCookieAuth()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/auth/check-auth");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var root = jsonDoc.RootElement;
                    
                    if (root.TryGetProperty("isAuthenticated", out var authElement) && authElement.GetBoolean())
                    {
                        // Lưu thông tin user nếu chưa có
                        if (root.TryGetProperty("user", out var userElement))
                        {
                            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CookieAuthKey, userElement.GetRawText());
                        }
                        
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authType", "cookie");
                        OnAuthStateChanged?.Invoke();
                        return new AuthResultDto { IsSuccess = true };
                    }
                    
                    return new AuthResultDto { IsSuccess = false, ErrorMessage = "Chưa đăng nhập" };
                }
                catch (JsonException)
                {
                    return new AuthResultDto { IsSuccess = false, ErrorMessage = "Phản hồi từ server không hợp lệ." };
                }
            }

            return new AuthResultDto { IsSuccess = false, ErrorMessage = "Không thể kiểm tra trạng thái đăng nhập" };
        }
        catch (Exception ex)
        {
            return new AuthResultDto { IsSuccess = false, ErrorMessage = $"Lỗi: {ex.Message}" };
        }
    }

    // Đăng xuất cookie
    public async Task LogoutCookie()
    {
        var response = await _httpClient.PostAsync("api/auth/logout-cookie", null);

        if (response.IsSuccessStatusCode)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", CookieAuthKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authType");
            OnAuthStateChanged?.Invoke();
            _navigationManager.NavigateTo("/login");
        }
    }

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
        var authType = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authType");
        
        if (authType == "cookie")
        {
            await LogoutCookie();
            return;
        }

        var role = await GetUserRoleFromToken();
        var response = await _httpClient.PostAsync("api/auth/logout", null);

        if (response.IsSuccessStatusCode)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StudentInfoKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authType");

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
            // Logout failed
        }
    }

    public async Task<bool> IsAuthenticated()
    {
        var authType = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authType");
        
        if (authType == "cookie")
        {
            var cookieAuth = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", CookieAuthKey);
            return !string.IsNullOrEmpty(cookieAuth);
        }
        
        var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
        return !string.IsNullOrEmpty(token);
    }

    public async Task<string?> GetUserRoleFromToken()
    {
        var authType = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authType");
        
        if (authType == "cookie")
        {
            var cookieAuthJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", CookieAuthKey);
            if (!string.IsNullOrEmpty(cookieAuthJson))
            {
                try
                {
                    var userInfo = JsonDocument.Parse(cookieAuthJson);
                    if (userInfo.RootElement.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array)
                    {
                        var roles = rolesElement.EnumerateArray().Select(r => r.GetString()).ToList();
                        return roles.FirstOrDefault();
                    }
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

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
        // Kiểm tra cookie auth trước
        var authType = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authType");
        if (authType == "cookie")
        {
            await CheckCookieAuth();
        }
        else
        {
            // Không cần thêm token vào header nữa vì đã có AuthHeaderHandler
            OnAuthStateChanged?.Invoke();
        }
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
