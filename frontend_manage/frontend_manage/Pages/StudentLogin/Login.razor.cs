using frontend_manage.DTOs;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;

namespace frontend_manage.Pages.StudentLogin;

public partial class Login : ComponentBase
{
    [Inject] private AuthService AuthService { get; set; }
    [Inject] private NavigationManager Navigation { get; set; }

    private LoginModelDto loginModel = new();
    private string? ErrorMessage;
    private bool _isLoading = false;

    private async Task HandleLogin()
    {
        ErrorMessage = null;
        _isLoading = true;
        StateHasChanged();
        
        try
        {
            var result = await AuthService.LoginStudent(loginModel.UserName, loginModel.Password);
            
            if (result.IsSuccess)
            {
                Navigation.NavigateTo("/student-dashboard");
            }
            else
            {
                if (result.ErrorMessage == "Không tìm thấy sinh viên với mã này.")
                {
                    ErrorMessage = "Không tìm thấy thông tin thí sinh, vui lòng liên hệ cán bộ coi thi";
                }
                else
                {
                    ErrorMessage = result.ErrorMessage;
                }
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