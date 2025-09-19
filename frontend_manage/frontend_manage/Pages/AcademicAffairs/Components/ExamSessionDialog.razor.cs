using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using frontend_manage.Pages.AcademicAffairs.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class ExamSessionDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private ExamSessionService ExamSessionService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public ExamSessionCreateDto ExamSession { get; set; } = new();
    [Parameter] public List<ExamBatchDto> ExamBatches { get; set; } = new();
    [Parameter] public bool IsEdit { get; set; } = false;
    [Parameter] public int? ExamSessionId { get; set; }

    private MudForm _form = null!;
    private bool _isValid = true;
    private string[] _errors = Array.Empty<string>();
    private bool _isLoading = false;
    private ExamSessionCreateDto _examSession = new();
    private TimeSpan? _startTime;
    private TimeSpan? _endTime;

    protected override void OnInitialized()
    {
        _examSession = ExamSession;
        _startTime = ExamSession.StartTime.TimeOfDay;
        _endTime = ExamSession.EndTime.TimeOfDay;
    }

    private async Task SaveExamSession()
    {
        if (!_isValid) return;

        if (_startTime.HasValue)
        {
            var startDate = _examSession.StartTime.Date.Add(_startTime.Value);
            _examSession.StartTime = startDate;
        }
        if (_endTime.HasValue)
        {
            var endDate = _examSession.EndTime.Date.Add(_endTime.Value);
            _examSession.EndTime = endDate;
        }

        _isLoading = true;
        StateHasChanged();

        try
        {
            if (IsEdit && ExamSessionId.HasValue)
            {
                var updateDto = new ExamSessionUpdateDto
                {
                    Name = _examSession.Name,
                    StartTime = _examSession.StartTime,
                    EndTime = _examSession.EndTime,
                    IsActive = _examSession.IsActive,
                    IsCompleted = _examSession.IsCompleted,
                    ExamBatchDetailId = _examSession.ExamBatchDetailId
                };
                await ExamSessionService.UpdateAsync(ExamSessionId.Value, updateDto);
                Snackbar.Add("Cập nhật ca thi thành công", Severity.Success);
            }
            else
            {
                await ExamSessionService.CreateAsync(_examSession);
                Snackbar.Add("Thêm ca thi thành công", Severity.Success);
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
