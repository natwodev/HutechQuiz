using frontend_manage.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;

namespace frontend_manage.Pages.Monitor.Components
{
    public partial class AddExtraMinutesDialog : ComponentBase
    {
        [CascadingParameter]
        public IMudDialogInstance MudDialog { get; set; } = default!;  // Sửa: Dùng MudDialogInstance (chuẩn MudBlazor)

    [Inject]
    public MonitorService MonitorService { get; set; } = default!;

        [Inject]
        public ISnackbar Snackbar { get; set; } = default!;

        [Parameter]
        public string StudentCode { get; set; } = string.Empty;

        [Parameter]
        public int StudentExamSessionId { get; set; }

        [Parameter]
        public int TotalExamMinutes { get; set; } = 60;

        private int ExtraMinutes { get; set; } = 5;
        private string Reason { get; set; } = string.Empty;
        private bool IsSubmitting { get; set; } = false;
        private int MaxExtraMinutes => (int)Math.Floor(TotalExamMinutes * 0.3);
        private bool IsValid => ExtraMinutes > 0 && ExtraMinutes <= MaxExtraMinutes;

        public void Cancel()  // Đổi thành synchronous, không cần async vì MudDialogInstance dùng Cancel()
        {
            MudDialog.Cancel();  // Đúng: Gọi Cancel() trực tiếp trên MudDialogInstance
        }

        private async Task Submit()  // Giữ async vì có gọi service async
        {
            if (!IsValid || IsSubmitting)
            {
                Snackbar.Add($"Số phút cộng thêm không được vượt quá {MaxExtraMinutes} phút (30% tổng thời gian)", Severity.Error);
                return;
            }
            IsSubmitting = true;
            try
            {
                var result = await MonitorService.AddExtraMinutesAsync(StudentCode, StudentExamSessionId, ExtraMinutes, string.IsNullOrWhiteSpace(Reason) ? null : Reason);
                Snackbar.Add(result, Severity.Success);
                MudDialog.Close(DialogResult.Ok(new { ExtraMinutes, Reason }));
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}
