using frontend_manage.DTOs;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;

namespace frontend_manage.Pages.Monitor;

public partial class Login
{
    // Service injections are handled in the Razor file

    private LoginModelDto loginModel = new();
    private string? ErrorMessage;
    private bool IsLoading = false;

    protected override async Task OnInitializedAsync()
    {
        // Kiểm tra xem đã đăng nhập chưa
        if (await AuthService.IsAuthenticated())
        {
            var role = await AuthService.GetUserRoleFromToken();
            if (role == "Admin" || role == "ITManager" || role == "ExamManager")
            {
                Navigation.NavigateTo("/monitor/dashboard");
            }
        }
    }

    private async Task HandleLogin()
    {
        ErrorMessage = null;
        IsLoading = true;
        
        try
        {
            // Sử dụng JWT authentication cho monitor
            var result = await AuthService.Login(loginModel.UserName, loginModel.Password);
            
            if (result.IsSuccess)
            {
                var role = await AuthService.GetUserRoleFromToken();
                
                // Chỉ cho phép các role có quyền truy cập monitor
                if (role == "Admin" || role == "ITManager" || role == "ExamManager")
                {
                    Navigation.NavigateTo("/monitor/dashboard");
                }
                else
                {
                    ErrorMessage = "Bạn không có quyền truy cập hệ thống giám sát.";
                    await AuthService.Logout(); // Đăng xuất nếu không có quyền
                }
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi đăng nhập: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
