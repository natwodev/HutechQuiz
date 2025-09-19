using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using frontend_manage.Pages.AcademicAffairs.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class ExamBatchDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private ExamBatchService ExamBatchService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public ExamBatchCreateDto ExamBatch { get; set; } = new();
    [Parameter] public List<SemesterDto> Semesters { get; set; } = new();
    [Parameter] public bool IsEdit { get; set; } = false;
    [Parameter] public int? ExamBatchId { get; set; }

    private MudForm _form = null!;
    private bool _isValid = true;
    private string[] _errors = Array.Empty<string>();
    private bool _isLoading = false;
    private ExamBatchCreateDto _examBatch = new();
    private DateTime? _startDate;
    private DateTime? _endDate;

    protected override void OnInitialized()
    {
        _examBatch = ExamBatch;
        _startDate = ExamBatch.StartDate;
        _endDate = ExamBatch.EndDate;
    }

    private async Task SaveExamBatch()
    {
        if (!_isValid) return;

        if (_startDate.HasValue)
            _examBatch.StartDate = _startDate.Value;
        if (_endDate.HasValue)
            _examBatch.EndDate = _endDate.Value;

        _isLoading = true;
        StateHasChanged();

        try
        {
            if (IsEdit && ExamBatchId.HasValue)
            {
                var updateDto = new ExamBatchUpdateDto
                {
                    BatchName = _examBatch.BatchName,
                    Description = _examBatch.Description,
                    StartDate = _examBatch.StartDate,
                    EndDate = _examBatch.EndDate,
                    SemesterId = _examBatch.SemesterId,
                    IsActive = _examBatch.IsActive,
                    ExamBatchDetails = _examBatch.ExamBatchDetails.Select(d => new ExamBatchDetailUpdateDto
                    {
                        Name = d.Name,
                        ExamBatchId = d.ExamBatchId,
                        ExamSessions = d.ExamSessions.Select(s => new ExamSessionUpdateDto
                        {
                            Name = s.Name,
                            StartTime = s.StartTime,
                            EndTime = s.EndTime,
                            IsActive = s.IsActive,
                            IsCompleted = s.IsCompleted,
                            ExamBatchDetailId = s.ExamBatchDetailId
                        }).ToList()
                    }).ToList()
                };
                await ExamBatchService.UpdateAsync(ExamBatchId.Value, updateDto);
                Snackbar.Add("Cập nhật đợt thi thành công", Severity.Success);
            }
            else
            {
                await ExamBatchService.CreateAsync(_examBatch);
                Snackbar.Add("Thêm đợt thi thành công", Severity.Success);
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
