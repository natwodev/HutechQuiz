using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using frontend_manage.Pages.AcademicAffairs.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class AcademicYearTab : ComponentBase
{
    [Inject] private AcademicYearService AcademicYearService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public List<AcademicYearDto> AcademicYears { get; set; } = new();
    [Parameter] public EventCallback OnAcademicYearUpdated { get; set; }

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters<AcademicYearDialog>
        {
            { "AcademicYear", new AcademicYearCreateDto() },
            { "IsEdit", false }
        };

        var dialog = await DialogService.ShowAsync<AcademicYearDialog>("Thêm năm học mới", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await OnAcademicYearUpdated.InvokeAsync();
        }
    }

    private async Task EditAcademicYear(AcademicYearDto academicYear)
    {
        var updateDto = new AcademicYearUpdateDto
        {
            YearName = academicYear.YearName
        };

        var parameters = new DialogParameters<AcademicYearDialog>
        {
            { "AcademicYear", updateDto },
            { "IsEdit", true },
            { "AcademicYearId", academicYear.AcademicYearId }
        };

        var dialog = await DialogService.ShowAsync<AcademicYearDialog>("Chỉnh sửa năm học", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await OnAcademicYearUpdated.InvokeAsync();
        }
    }

    private async Task DeleteAcademicYear(AcademicYearDto academicYear)
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { "Title", "Xác nhận xóa" },
            { "Message", $"Bạn có chắc chắn muốn xóa năm học '{academicYear.YearName}'?" },
            { "ConfirmText", "Xóa" }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Xác nhận xóa", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            try
            {
                await AcademicYearService.DeleteAsync(academicYear.AcademicYearId);
                Snackbar.Add("Xóa năm học thành công", Severity.Success);
                await OnAcademicYearUpdated.InvokeAsync();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi khi xóa năm học: {ex.Message}", Severity.Error);
            }
        }
    }
}



