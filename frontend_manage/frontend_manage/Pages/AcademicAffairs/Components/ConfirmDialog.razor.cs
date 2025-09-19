using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class ConfirmDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public string Title { get; set; } = "Xác nhận";
    [Parameter] public string Message { get; set; } = "Bạn có chắc chắn muốn thực hiện hành động này?";
    [Parameter] public string ConfirmText { get; set; } = "Xác nhận";

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private void Confirm()
    {
        MudDialog.Close(DialogResult.Ok(true));
    }
}



