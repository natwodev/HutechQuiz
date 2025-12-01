using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.Admin;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class SemesterDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public SemesterDto? Semester { get; set; }
    [Parameter] public List<AcademicYearDto> AcademicYears { get; set; } = new();
    
    [Inject] private AdminSemesterService SemesterService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    
    private SemesterCreateDto semester = new();
    private bool isEdit = false;

    protected override void OnInitialized()
    {
        if (AcademicYears.Count > 0 && semester.AcademicYearId == 0)
        {
            semester.AcademicYearId = AcademicYears.First().AcademicYearId;
        }

        if (Semester != null)
        {
            isEdit = true;
            semester.SemesterName = Semester.SemesterName;
            semester.AcademicYearId = Semester.AcademicYearId;
        }
    }

    private async Task HandleSubmit()
    {
        try
        {
            if (semester.AcademicYearId == 0)
            {
                Snackbar.Add("Vui lòng chọn năm học", Severity.Error);
                return;
            }

            if (isEdit && Semester != null)
            {
                var updateDto = new SemesterUpdateDto 
                { 
                    SemesterName = semester.SemesterName,
                    AcademicYearId = semester.AcademicYearId
                };
                await SemesterService.UpdateAsync(Semester.SemesterId, updateDto);
                Snackbar.Add("Cập nhật học kỳ thành công", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                await SemesterService.CreateAsync(semester);
                Snackbar.Add("Thêm học kỳ thành công", Severity.Success);
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

