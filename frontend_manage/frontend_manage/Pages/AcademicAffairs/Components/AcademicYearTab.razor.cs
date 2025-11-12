using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class AcademicYearTab : ComponentBase
{
    [Inject] private AcademicYearService AcademicYearService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    [Parameter]
    public List<AcademicYearDto> AcademicYears { get; set; } = new();
    
    [Parameter]
    public EventCallback OnAcademicYearUpdated { get; set; }
    
    private string searchString = "";

    private bool FilterFunc(AcademicYearDto academicYear)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;
        
        return academicYear.YearName.Contains(searchString, StringComparison.OrdinalIgnoreCase);
    }

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters { ["AcademicYear"] = (AcademicYearDto?)null };
        var dialog = await DialogService.ShowAsync<AcademicYearDialog>("Thêm năm học mới", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnAcademicYearUpdated.InvokeAsync();
        }
    }

    private async Task OpenEditDialog(AcademicYearDto academicYear)
    {
        var parameters = new DialogParameters { ["AcademicYear"] = academicYear };
        var dialog = await DialogService.ShowAsync<AcademicYearDialog>("Sửa năm học", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnAcademicYearUpdated.InvokeAsync();
        }
    }

    private async Task OpenDeleteDialog(AcademicYearDto academicYear)
    {
        var parameters = new DialogParameters 
        { 
            ["Message"] = $"Bạn có chắc chắn muốn xóa năm học '{academicYear.YearName}'?" 
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Xác nhận xóa", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled)
        {
            await DeleteAcademicYear(academicYear);
        }
    }

    private async Task DeleteAcademicYear(AcademicYearDto academicYear)
    {
        try
        {
            await AcademicYearService.DeleteAsync(academicYear.AcademicYearId);
            Snackbar.Add("Xóa năm học thành công", Severity.Success);
            await OnAcademicYearUpdated.InvokeAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }
}
