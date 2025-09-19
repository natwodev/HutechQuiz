using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using frontend_manage.Pages.AcademicAffairs.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class ExamSessionTab : ComponentBase
{
    [Inject] private ExamSessionService ExamSessionService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public List<ExamSessionDto> ExamSessions { get; set; } = new();
    [Parameter] public List<ExamBatchDto> ExamBatches { get; set; } = new();
    [Parameter] public EventCallback OnExamSessionUpdated { get; set; }

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters<ExamSessionDialog>
        {
            { "ExamSession", new ExamSessionCreateDto() },
            { "ExamBatches", ExamBatches },
            { "IsEdit", false }
        };

        var dialog = await DialogService.ShowAsync<ExamSessionDialog>("Thêm ca thi mới", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await OnExamSessionUpdated.InvokeAsync();
        }
    }

    private async Task EditExamSession(ExamSessionDto examSession)
    {
        var updateDto = new ExamSessionUpdateDto
        {
            Name = examSession.Name,
            StartTime = examSession.StartTime,
            EndTime = examSession.EndTime,
            IsActive = examSession.IsActive,
            IsCompleted = examSession.IsCompleted,
            ExamBatchDetailId = examSession.ExamBatchDetailId
        };

        var parameters = new DialogParameters<ExamSessionDialog>
        {
            { "ExamSession", updateDto },
            { "ExamBatches", ExamBatches },
            { "IsEdit", true },
            { "ExamSessionId", examSession.ExamSessionId }
        };

        var dialog = await DialogService.ShowAsync<ExamSessionDialog>("Chỉnh sửa ca thi", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await OnExamSessionUpdated.InvokeAsync();
        }
    }

    private async Task DeleteExamSession(ExamSessionDto examSession)
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { "Title", "Xác nhận xóa" },
            { "Message", $"Bạn có chắc chắn muốn xóa ca thi '{examSession.Name}'?" },
            { "ConfirmText", "Xóa" }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Xác nhận xóa", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            try
            {
                await ExamSessionService.DeleteAsync(examSession.ExamSessionId);
                Snackbar.Add("Xóa ca thi thành công", Severity.Success);
                await OnExamSessionUpdated.InvokeAsync();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi khi xóa ca thi: {ex.Message}", Severity.Error);
            }
        }
    }
}



