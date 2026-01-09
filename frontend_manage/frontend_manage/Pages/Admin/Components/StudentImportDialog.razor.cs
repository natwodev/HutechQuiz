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
    
    [Parameter] public string DefaultExamSessionSubjectCore { get; set; } = "";

    private IBrowserFile? selectedFile;
    private string examSessionSubjectCore = "";

    protected override void OnInitialized()
    {
        if (!string.IsNullOrEmpty(DefaultExamSessionSubjectCore))
        {
            examSessionSubjectCore = DefaultExamSessionSubjectCore;
        }
    }

    private void OnInputFileChange(InputFileChangeEventArgs e)
    {
        Console.WriteLine($"File selected: {e.File.Name}, Size: {e.File.Size}");
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
        Console.WriteLine("HandleImport called");
        Console.WriteLine($"Core: {examSessionSubjectCore}, File: {selectedFile?.Name}");

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

