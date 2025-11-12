using Microsoft.AspNetCore.Components;
using frontend_manage.Services;

namespace frontend_manage.Pages.ExamManager;

public partial class Dashboard : ComponentBase
{
    [Inject] private AuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private async Task Logout()
    {
        await AuthService.Logout();
        NavigationManager.NavigateTo("/login");
    }
}


