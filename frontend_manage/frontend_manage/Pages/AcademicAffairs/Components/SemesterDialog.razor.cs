using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using frontend_manage.Pages.AcademicAffairs.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class SemesterDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private SemesterService SemesterService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public SemesterCreateDto Semester { get; set; } = new();
    [Parameter] public List<AcademicYearDto> AcademicYears { get; set; } = new();
    [Parameter] public bool IsEdit { get; set; } = false;
    [Parameter] public int? SemesterId { get; set; }

    private MudForm _form = null!;
    private bool _isValid = true;
    private string[] _errors = Array.Empty<string>();
    private bool _isLoading = false;
    private SemesterCreateDto _semester = new();

    protected override void OnInitialized()
    {
        _semester = Semester;
    }

    private async Task SaveSemester()
    {
        if (!_isValid) return;

        _isLoading = true;
        StateHasChanged();

        try
        {
            if (IsEdit && SemesterId.HasValue)
            {
                var updateDto = new SemesterUpdateDto
                {
                    SemesterName = _semester.SemesterName,
                    AcademicYearId = _semester.AcademicYearId
                };
                await SemesterService.UpdateAsync(SemesterId.Value, updateDto);
                Snackbar.Add("Cập nhật học kỳ thành công", Severity.Success);
            }
            else
            {
                await SemesterService.CreateAsync(_semester);
                Snackbar.Add("Thêm học kỳ thành công", Severity.Success);
            }

            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }
}
