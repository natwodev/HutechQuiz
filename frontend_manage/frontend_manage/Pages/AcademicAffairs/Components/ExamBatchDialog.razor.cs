using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class ExamBatchDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public ExamBatchDto? ExamBatch { get; set; }
    [Parameter] public List<SemesterDto> Semesters { get; set; } = new();
    
    [Inject] private ExamBatchService ExamBatchService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    
    private ExamBatchCreateDto examBatch = new();
    private bool isEdit = false;

    protected override void OnInitialized()
    {
        if (ExamBatch != null)
        {
            isEdit = true;
            examBatch.BatchName = ExamBatch.BatchName;
            examBatch.SemesterId = ExamBatch.SemesterId;
        }
    }

    private async Task HandleSubmit()
    {
        try
        {
            if (isEdit && ExamBatch != null)
            {
                var updateDto = new ExamBatchUpdateDto 
                { 
                    BatchName = examBatch.BatchName,
                    SemesterId = examBatch.SemesterId
                };
                await ExamBatchService.UpdateAsync(ExamBatch.ExamBatchId, updateDto);
                Snackbar.Add("Cập nhật đợt thi thành công", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                await ExamBatchService.CreateAsync(examBatch);
                Snackbar.Add("Thêm đợt thi thành công", Severity.Success);
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
