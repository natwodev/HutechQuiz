using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using frontend_manage.Services;

namespace frontend_manage.Pages.Login;

public partial class Login : ComponentBase
{
    [Inject] private AuthService AuthService { get; set; }
    [Inject] private NavigationManager Navigation { get; set; }

    private FrontEnd.DTOs.LoginModelDto loginModel = new();
    private string? ErrorMessage;
    private bool _processing = false;

    private async Task HandleLogin()
    {
        ErrorMessage = null;
        _processing = true;
        StateHasChanged();
        var result = await AuthService.LoginStudent(loginModel.UserName, loginModel.Password);
        _processing = false;
        StateHasChanged();
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
}