using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class SemesterTab : ComponentBase
{
    [Inject] private SemesterService SemesterService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    [Parameter]
    public List<SemesterDto> Semesters { get; set; } = new();
    
    [Parameter]
    public List<AcademicYearDto> AcademicYears { get; set; } = new();
    
    [Parameter]
    public EventCallback OnSemesterUpdated { get; set; }
    
    private string searchString = "";

    private bool FilterFunc(SemesterDto semester)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;
        
        return semester.SemesterName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
               GetAcademicYearName(semester.AcademicYearId).Contains(searchString, StringComparison.OrdinalIgnoreCase);
    }

    private string GetAcademicYearName(int academicYearId)
    {
        var academicYear = AcademicYears.FirstOrDefault(ay => ay.AcademicYearId == academicYearId);
        return academicYear?.YearName ?? "N/A";
    }

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters { ["Semester"] = (SemesterDto?)null, ["AcademicYears"] = AcademicYears };
        var dialog = await DialogService.ShowAsync<SemesterDialog>("Thêm học kỳ mới", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnSemesterUpdated.InvokeAsync();
        }
    }

    private async Task OpenEditDialog(SemesterDto semester)
    {
        var parameters = new DialogParameters { ["Semester"] = semester, ["AcademicYears"] = AcademicYears };
        var dialog = await DialogService.ShowAsync<SemesterDialog>("Sửa học kỳ", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnSemesterUpdated.InvokeAsync();
        }
    }

    private async Task OpenDeleteDialog(SemesterDto semester)
    {
        var parameters = new DialogParameters 
        { 
            ["Message"] = $"Bạn có chắc chắn muốn xóa học kỳ '{semester.SemesterName}'?" 
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Xác nhận xóa", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled)
        {
            await DeleteSemester(semester);
        }
    }

    private async Task DeleteSemester(SemesterDto semester)
    {
        try
        {
            await SemesterService.DeleteAsync(semester.SemesterId);
            Snackbar.Add("Xóa học kỳ thành công", Severity.Success);
            await OnSemesterUpdated.InvokeAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }
}

