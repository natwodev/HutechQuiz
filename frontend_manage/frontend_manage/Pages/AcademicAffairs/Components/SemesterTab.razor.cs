using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using frontend_manage.Pages.AcademicAffairs.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class SemesterTab : ComponentBase
{
    [Inject] private SemesterService SemesterService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public List<SemesterDto> Semesters { get; set; } = new();
    [Parameter] public List<AcademicYearDto> AcademicYears { get; set; } = new();
    [Parameter] public EventCallback OnSemesterUpdated { get; set; }

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters<SemesterDialog>
        {
            { "Semester", new SemesterCreateDto() },
            { "AcademicYears", AcademicYears },
            { "IsEdit", false }
        };

        var dialog = await DialogService.ShowAsync<SemesterDialog>("Thêm học kỳ mới", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await OnSemesterUpdated.InvokeAsync();
        }
    }

    private async Task EditSemester(SemesterDto semester)
    {
        var updateDto = new SemesterUpdateDto
        {
            SemesterName = semester.SemesterName,
            AcademicYearId = semester.AcademicYearId
        };

        var parameters = new DialogParameters<SemesterDialog>
        {
            { "Semester", updateDto },
            { "AcademicYears", AcademicYears },
            { "IsEdit", true },
            { "SemesterId", semester.SemesterId }
        };

        var dialog = await DialogService.ShowAsync<SemesterDialog>("Chỉnh sửa học kỳ", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await OnSemesterUpdated.InvokeAsync();
        }
    }

    private async Task DeleteSemester(SemesterDto semester)
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { "Title", "Xác nhận xóa" },
            { "Message", $"Bạn có chắc chắn muốn xóa học kỳ '{semester.SemesterName}'?" },
            { "ConfirmText", "Xóa" }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Xác nhận xóa", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            try
            {
                await SemesterService.DeleteAsync(semester.SemesterId);
                Snackbar.Add("Xóa học kỳ thành công", Severity.Success);
                await OnSemesterUpdated.InvokeAsync();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi khi xóa học kỳ: {ex.Message}", Severity.Error);
            }
        }
    }
}



