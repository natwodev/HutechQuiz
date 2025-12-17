using Microsoft.AspNetCore.Components;
using frontend_manage.Services;

namespace frontend_manage.Pages.ExamManager;

public partial class Dashboard : ComponentBase
{
    [Inject] private AuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private int activeTab = 0;
    private int previousTab = 0;

    private void ActivateTab(int tabIndex)
    {
        previousTab = activeTab;
        activeTab = tabIndex;
    }

    private async Task Logout()
    {
        await AuthService.Logout();
        NavigationManager.NavigateTo("/login");
    }
}