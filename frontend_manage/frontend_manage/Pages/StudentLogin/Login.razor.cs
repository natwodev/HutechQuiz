using frontend_manage.DTOs;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace frontend_manage.Pages.StudentLogin;

public partial class Login : ComponentBase
{
    [Inject] private AuthService AuthService { get; set; }
    [Inject] private NavigationManager Navigation { get; set; }

    private StudentLoginRequestDto loginModel = new();
    private string? ErrorMessage;
    private bool _isLoading = false;

    private async Task HandleLogin()
    {
        ErrorMessage = null;
        _isLoading = true;
        StateHasChanged();
        
        try
        {
            // Kiểm tra validation trước khi gọi API
            if (string.IsNullOrWhiteSpace(loginModel.StudentCode1))
            {
                ErrorMessage = "Vui lòng nhập mã sinh viên";
                return;
            }
            
            if (string.IsNullOrWhiteSpace(loginModel.StudentCode2))
            {
                ErrorMessage = "Vui lòng nhập mật khẩu";
                return;
            }
            
            // Kiểm tra format (chỉ chữ cái và số)
            if (!System.Text.RegularExpressions.Regex.IsMatch(loginModel.StudentCode1, @"^[a-zA-Z0-9]+$"))
            {
                ErrorMessage = "Mã sinh viên chỉ được chứa chữ cái và số";
                return;
            }
            
            if (!System.Text.RegularExpressions.Regex.IsMatch(loginModel.StudentCode2, @"^[a-zA-Z0-9]+$"))
            {
                ErrorMessage = "Mã sinh viên chỉ được chứa chữ cái và số";
                return;
            }
            
            if (loginModel.StudentCode1 != loginModel.StudentCode2)
            {
                ErrorMessage = "Mã sinh viên nhập không khớp. Vui lòng kiểm tra lại.";
                return;
            }
            
            var result = await AuthService.LoginStudent(loginModel.StudentCode1, loginModel.StudentCode2);
            
            if (result.IsSuccess)
            {
                Navigation.NavigateTo("/student-dashboard");
            }
            else
            {
                // Xử lý các thông báo lỗi từ backend
                ErrorMessage = result.ErrorMessage switch
                {
                    "Không tìm thấy sinh viên với mã này." => "Không tìm thấy thông tin thí sinh, vui lòng liên hệ cán bộ coi thi",
                    "Mã sinh viên nhập không khớp." => "Mã sinh viên nhập không khớp. Vui lòng kiểm tra lại.",
                    _ => result.ErrorMessage
                };
            }
        }
        catch (System.Exception ex)
        {
            ErrorMessage = "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại.";
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }
}