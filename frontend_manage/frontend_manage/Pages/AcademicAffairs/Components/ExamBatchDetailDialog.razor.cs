using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class ExamBatchDetailDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public ExamBatchDetailDto? ExamBatchDetail { get; set; }
    [Parameter] public List<ExamBatchDto> ExamBatches { get; set; } = new();
    
    [Inject] private ExamBatchDetailService ExamBatchDetailService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    
    private ExamBatchDetailCreateDto examBatchDetail = new();
    private bool isEdit = false;

    protected override void OnInitialized()
    {
        if (ExamBatchDetail != null)
        {
            isEdit = true;
            examBatchDetail.Name = ExamBatchDetail.Name;
            examBatchDetail.ExamBatchId = ExamBatchDetail.ExamBatchId;
        }
    }

    private async Task HandleSubmit()
    {
        try
        {
            if (isEdit && ExamBatchDetail != null)
            {
                var updateDto = new ExamBatchDetailUpdateDto 
                { 
                    Name = examBatchDetail.Name,
                    ExamBatchId = examBatchDetail.ExamBatchId
                };
                await ExamBatchDetailService.UpdateAsync(ExamBatchDetail.ExamBatchDetailId, updateDto);
                Snackbar.Add("Cập nhật chi tiết đợt thi thành công", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                await ExamBatchDetailService.CreateAsync(examBatchDetail);
                Snackbar.Add("Thêm chi tiết đợt thi thành công", Severity.Success);
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
