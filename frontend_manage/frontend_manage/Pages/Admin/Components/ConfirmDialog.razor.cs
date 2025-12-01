using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class ConfirmDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    
    [Parameter] public string ContentText { get; set; } = "Bạn có chắc chắn muốn thực hiện hành động này?";
    [Parameter] public string ButtonText { get; set; } = "Xác nhận";
    [Parameter] public Color Color { get; set; } = Color.Error;

    void Submit() => MudDialog.Close(DialogResult.Ok(true));
    void Cancel() => MudDialog.Cancel();
}


