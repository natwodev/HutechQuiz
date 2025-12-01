using frontend_manage.DTOs;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.Admin;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class LecturerManagementTab : ComponentBase
{
    [Parameter]
    public List<LecturerDto> Lecturers { get; set; } = new();
    
    [Parameter]
    public List<DepartmentDto> Departments { get; set; } = new();
    
    [Parameter]
    public EventCallback OnLecturerUpdated { get; set; }
    
    [Inject] private LecturerService LecturerService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    
    private string searchString = "";

    private bool FilterFunc(LecturerDto lecturer)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;
        
        return lecturer.LecturerCode.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
               lecturer.FirstName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
               lecturer.LastName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
               lecturer.Email.Contains(searchString, StringComparison.OrdinalIgnoreCase);
    }

    private async Task OpenImportDialog()
    {
        var dialog = await DialogService.ShowAsync<LecturerImportDialog>("Import giảng viên từ Excel");
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnLecturerUpdated.InvokeAsync();
        }
    }

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters { ["Lecturer"] = (LecturerDto?)null, ["Departments"] = Departments };
        var dialog = await DialogService.ShowAsync<LecturerDialog>("Thêm giảng viên mới", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnLecturerUpdated.InvokeAsync();
        }
    }

    private async Task OpenEditDialog(LecturerDto lecturer)
    {
        var parameters = new DialogParameters { ["Lecturer"] = lecturer, ["Departments"] = Departments };
        var dialog = await DialogService.ShowAsync<LecturerDialog>("Sửa giảng viên", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnLecturerUpdated.InvokeAsync();
        }
    }

    private async Task OpenDeleteDialog(LecturerDto lecturer)
    {
        var parameters = new DialogParameters 
        { 
            ["Message"] = $"Bạn có chắc chắn muốn xóa giảng viên '{lecturer.FirstName} {lecturer.LastName}'?" 
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Xác nhận xóa", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled)
        {
            await DeleteLecturer(lecturer);
        }
    }

    private async Task DeleteLecturer(LecturerDto lecturer)
    {
        try
        {
            var success = await LecturerService.DeleteLecturerAsync(lecturer.LecturerCode);
            if (success)
            {
                Snackbar.Add("Xóa giảng viên thành công", Severity.Success);
                await OnLecturerUpdated.InvokeAsync();
            }
            else
            {
                Snackbar.Add("Lỗi khi xóa giảng viên", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }
}


