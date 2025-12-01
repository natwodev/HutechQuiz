using frontend_manage.DTOs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class StudentDetailsDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public StudentInfoDto? Student { get; set; }

    private void Close() => MudDialog.Close();
}


