using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class BaseDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public string Title { get; set; } = "Thêm mới";
    [Parameter] public string SaveText { get; set; } = "Lưu";
    [Parameter] public bool IsLoading { get; set; } = false;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback OnSave { get; set; }

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private void Save()
    {
        if (OnSave.HasDelegate)
        {
            OnSave.InvokeAsync();
        }
    }
}



