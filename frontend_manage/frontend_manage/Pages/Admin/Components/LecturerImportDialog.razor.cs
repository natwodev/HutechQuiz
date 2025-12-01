using frontend_manage.Services.Admin;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class LecturerImportDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    
    [Inject] private LecturerService LecturerService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    
    private IBrowserFile? selectedFile;
    private bool isDownloadingTemplate = false;

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
        if (selectedFile == null)
        {
            Snackbar.Add("Vui lòng chọn file Excel", Severity.Warning);
            return;
        }

        try
        {
            var result = await LecturerService.ImportFromExcelAsync(selectedFile);
            if (result != null)
            {
                Snackbar.Add($"Import thành công: {result.LecturersAdded} giảng viên đã được thêm", Severity.Success);
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

    private async Task DownloadTemplate()
    {
        isDownloadingTemplate = true;
        try
        {
            var success = await LecturerService.DownloadExcelTemplateAsync(JSRuntime);
            if (success)
            {
                Snackbar.Add("Đã tải mẫu file Excel thành công", Severity.Success);
            }
            else
            {
                Snackbar.Add("Lỗi khi tải mẫu file Excel", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
        finally
        {
            isDownloadingTemplate = false;
            StateHasChanged();
        }
    }

    private void Cancel() => MudDialog.Cancel();
}

