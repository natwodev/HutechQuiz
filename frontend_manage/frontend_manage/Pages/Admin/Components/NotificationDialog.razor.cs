using frontend_manage.Services.Admin;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class NotificationDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    
    [Inject] private SystemService SystemService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    
    private NotificationModel notificationModel = new();
    private bool isSending = false;

    private async Task HandleSend()
    {
        if (string.IsNullOrWhiteSpace(notificationModel.Message) || notificationModel.ExamTime == null)
        {
            Snackbar.Add("Vui lòng điền đầy đủ thông tin", Severity.Warning);
            return;
        }

        isSending = true;
        try
        {
            var success = await SystemService.SendNotificationAsync(notificationModel.Message, notificationModel.ExamTime.Value);
            if (success)
            {
                Snackbar.Add("Gửi thông báo thành công", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add("Lỗi khi gửi thông báo", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
        finally
        {
            isSending = false;
        }
    }

    private void Cancel() => MudDialog.Cancel();
}

public class NotificationModel
{
    public string Message { get; set; } = string.Empty;
    public DateTime? ExamTime { get; set; } = DateTime.Now;
}


