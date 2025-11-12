using frontend_manage.Services.Admin;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class StudentImportDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    
    [Inject] private AdminStudentService AdminStudentService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    
    private IBrowserFile? selectedFile;
    private string examSessionSubjectCore = "";

    private void OnInputFileChange(InputFileChangeEventArgs e)
    {
        selectedFile = e.File;
        StateHasChanged();
    }

    private void ClearFile()
    {
        selectedFile = null;
        StateHasChanged();
    }

    private async Task HandleImport()
    {
        if (selectedFile == null || string.IsNullOrEmpty(examSessionSubjectCore))
        {
            Snackbar.Add("Vui lòng chọn file và nhập mã ca thi môn học", Severity.Warning);
            return;
        }

        try
        {
            var result = await AdminStudentService.ImportFromExcelAsync(selectedFile, examSessionSubjectCore);
            if (result != null)
            {
                Snackbar.Add($"Import thành công: {result.StudentsAdded} sinh viên, {result.StudentExamSessionsAdded} phiên thi", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add("Lỗi khi import file", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }

    private void Cancel() => MudDialog.Cancel();
}

