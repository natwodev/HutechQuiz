using frontend_manage.Services.Admin;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class SystemManagementTab : ComponentBase
{
    [Parameter]
    public QueueStatusDto? QueueStatus { get; set; }
    
    [Parameter]
    public EventCallback OnSystemUpdated { get; set; }

    [Inject] private SystemService SystemService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private async Task CheckQueueStatus()
    {
        try
        {
            QueueStatus = await SystemService.CheckQueueStatusAsync();
            if (QueueStatus != null)
            {
                Snackbar.Add(QueueStatus.Message, QueueStatus.IsHealthy ? Severity.Success : Severity.Warning);
            }
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }

    private async Task OpenNotificationDialog()
    {
        var parameters = new DialogParameters();
        var dialog = await DialogService.ShowAsync<NotificationDialog>("Gửi thông báo", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnSystemUpdated.InvokeAsync();
        }
    }
}


