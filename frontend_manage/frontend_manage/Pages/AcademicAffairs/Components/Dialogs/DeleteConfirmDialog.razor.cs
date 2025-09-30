using MudBlazor;
using Microsoft.AspNetCore.Components;

namespace frontend_manage.Pages.AcademicAffairs.Components.Dialogs
{
    public partial class DeleteConfirmDialog
    {
        [CascadingParameter] private IMudDialogInstance? MudDialog { get; set; }
        
        [Parameter] public string Title { get; set; } = "Xác nhận xóa";
        [Parameter] public string Content { get; set; } = "Bạn có chắc chắn muốn xóa?";
        [Parameter] public string ConfirmText { get; set; } = "Xóa";
        [Parameter] public string CancelText { get; set; } = "Hủy";

        private void Cancel()
        {
            MudDialog?.Cancel();
        }

        private void Confirm()
        {
            MudDialog?.Close(DialogResult.Ok(true));
        }
    }
}


