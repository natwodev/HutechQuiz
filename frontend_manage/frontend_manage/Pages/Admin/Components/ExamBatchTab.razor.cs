using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.Admin;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class ExamBatchTab : ComponentBase
{
    [Inject] private AdminExamBatchService ExamBatchService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    [Parameter]
    public List<ExamBatchDto> ExamBatches { get; set; } = new();
    
    [Parameter]
    public List<SemesterDto> Semesters { get; set; } = new();
    
    [Parameter]
    public EventCallback OnExamBatchUpdated { get; set; }
    
    private string searchString = "";

    private bool FilterFunc(ExamBatchDto examBatch)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;
        
        return examBatch.BatchName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
               GetSemesterName(examBatch.SemesterId).Contains(searchString, StringComparison.OrdinalIgnoreCase);
    }

    private string GetSemesterName(int semesterId)
    {
        var semester = Semesters.FirstOrDefault(s => s.SemesterId == semesterId);
        return semester?.SemesterName ?? "N/A";
    }

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters { ["ExamBatch"] = (ExamBatchDto?)null, ["Semesters"] = Semesters };
        var dialog = await DialogService.ShowAsync<ExamBatchDialog>("Thêm đợt thi mới", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnExamBatchUpdated.InvokeAsync();
        }
    }

    private async Task OpenEditDialog(ExamBatchDto examBatch)
    {
        var parameters = new DialogParameters { ["ExamBatch"] = examBatch, ["Semesters"] = Semesters };
        var dialog = await DialogService.ShowAsync<ExamBatchDialog>("Sửa đợt thi", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnExamBatchUpdated.InvokeAsync();
        }
    }

    private async Task OpenDeleteDialog(ExamBatchDto examBatch)
    {
        var parameters = new DialogParameters 
        { 
            ["Message"] = $"Bạn có chắc chắn muốn xóa đợt thi '{examBatch.BatchName}'?" 
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Xác nhận xóa", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled)
        {
            await DeleteExamBatch(examBatch);
        }
    }

    private async Task DeleteExamBatch(ExamBatchDto examBatch)
    {
        try
        {
            await ExamBatchService.DeleteAsync(examBatch.ExamBatchId);
            Snackbar.Add("Xóa đợt thi thành công", Severity.Success);
            await OnExamBatchUpdated.InvokeAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }
}

