using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using frontend_manage.Pages.AcademicAffairs.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class AcademicYearDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private AcademicYearService AcademicYearService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public AcademicYearCreateDto AcademicYear { get; set; } = new();
    [Parameter] public bool IsEdit { get; set; } = false;
    [Parameter] public int? AcademicYearId { get; set; }

    private MudForm _form = null!;
    private bool _isValid = true;
    private string[] _errors = Array.Empty<string>();
    private bool _isLoading = false;
    private AcademicYearCreateDto _academicYear = new();

    protected override void OnInitialized()
    {
        _academicYear = AcademicYear;
    }

    private async Task SaveAcademicYear()
    {
        if (!_isValid) return;

        _isLoading = true;
        StateHasChanged();

        try
        {
            if (IsEdit && AcademicYearId.HasValue)
            {
                var updateDto = new AcademicYearUpdateDto
                {
                    YearName = _academicYear.YearName
                };
                await AcademicYearService.UpdateAsync(AcademicYearId.Value, updateDto);
                Snackbar.Add("Cập nhật năm học thành công", Severity.Success);
            }
            else
            {
                await AcademicYearService.CreateAsync(_academicYear);
                Snackbar.Add("Thêm năm học thành công", Severity.Success);
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



