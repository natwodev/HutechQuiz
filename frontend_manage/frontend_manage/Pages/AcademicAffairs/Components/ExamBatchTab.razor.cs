using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using frontend_manage.Pages.AcademicAffairs.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class ExamBatchTab : ComponentBase
{
    [Inject] private ExamBatchService ExamBatchService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public List<ExamBatchDto> ExamBatches { get; set; } = new();
    [Parameter] public List<SemesterDto> Semesters { get; set; } = new();
    [Parameter] public EventCallback OnExamBatchUpdated { get; set; }

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters<ExamBatchDialog>
        {
            { "ExamBatch", new ExamBatchCreateDto() },
            { "Semesters", Semesters },
            { "IsEdit", false }
        };

        var dialog = await DialogService.ShowAsync<ExamBatchDialog>("Thêm đợt thi mới", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await OnExamBatchUpdated.InvokeAsync();
        }
    }

    private async Task EditExamBatch(ExamBatchDto examBatch)
    {
        var updateDto = new ExamBatchUpdateDto
        {
            BatchName = examBatch.BatchName,
            Description = examBatch.Description,
            StartDate = examBatch.StartDate,
            EndDate = examBatch.EndDate,
            SemesterId = examBatch.SemesterId,
            IsActive = examBatch.IsActive,
            ExamBatchDetails = examBatch.ExamBatchDetails.Select(d => new ExamBatchDetailUpdateDto
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

        var parameters = new DialogParameters<ExamBatchDialog>
        {
            { "ExamBatch", updateDto },
            { "Semesters", Semesters },
            { "IsEdit", true },
            { "ExamBatchId", examBatch.ExamBatchId }
        };

        var dialog = await DialogService.ShowAsync<ExamBatchDialog>("Chỉnh sửa đợt thi", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await OnExamBatchUpdated.InvokeAsync();
        }
    }

    private async Task DeleteExamBatch(ExamBatchDto examBatch)
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { "Title", "Xác nhận xóa" },
            { "Message", $"Bạn có chắc chắn muốn xóa đợt thi '{examBatch.BatchName}'?" },
            { "ConfirmText", "Xóa" }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Xác nhận xóa", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            try
            {
                await ExamBatchService.DeleteAsync(examBatch.ExamBatchId);
                Snackbar.Add("Xóa đợt thi thành công", Severity.Success);
                await OnExamBatchUpdated.InvokeAsync();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi khi xóa đợt thi: {ex.Message}", Severity.Error);
            }
        }
    }
}



