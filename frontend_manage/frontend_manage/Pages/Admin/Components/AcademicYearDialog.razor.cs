using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.Admin;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class AcademicYearDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public AcademicYearDto? AcademicYear { get; set; }
    
    [Inject] private AdminAcademicYearService AcademicYearService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    
    private AcademicYearCreateDto academicYear = new();
    private bool isEdit = false;

    protected override void OnInitialized()
    {
        if (AcademicYear != null)
        {
            isEdit = true;
            academicYear.YearName = AcademicYear.YearName;
        }
    }

    private async Task HandleSubmit()
    {
        try
        {
            if (isEdit && AcademicYear != null)
            {
                var updateDto = new AcademicYearUpdateDto 
                { 
                    YearName = academicYear.YearName
                };
                await AcademicYearService.UpdateAsync(AcademicYear.AcademicYearId, updateDto);
                Snackbar.Add("Cập nhật năm học thành công", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                await AcademicYearService.CreateAsync(academicYear);
                Snackbar.Add("Thêm năm học thành công", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }

    private void Cancel() => MudDialog.Cancel();
}

