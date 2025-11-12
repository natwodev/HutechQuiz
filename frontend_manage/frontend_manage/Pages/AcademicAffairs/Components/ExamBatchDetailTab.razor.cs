using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class ExamBatchDetailTab : ComponentBase
{
    [Inject] private ExamBatchDetailService ExamBatchDetailService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    [Parameter]
    public List<ExamBatchDetailDto> ExamBatchDetails { get; set; } = new();
    
    [Parameter]
    public List<ExamBatchDto> ExamBatches { get; set; } = new();
    
    [Parameter]
    public EventCallback OnExamBatchDetailUpdated { get; set; }
    
    private string searchString = "";

    private bool FilterFunc(ExamBatchDetailDto examBatchDetail)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;
        
        return examBatchDetail.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
               examBatchDetail.ExamBatchName?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true;
    }

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters { ["ExamBatchDetail"] = (ExamBatchDetailDto?)null, ["ExamBatches"] = ExamBatches };
        var dialog = await DialogService.ShowAsync<ExamBatchDetailDialog>("Thêm chi tiết đợt thi mới", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnExamBatchDetailUpdated.InvokeAsync();
        }
    }

    private async Task OpenEditDialog(ExamBatchDetailDto examBatchDetail)
    {
        var parameters = new DialogParameters { ["ExamBatchDetail"] = examBatchDetail, ["ExamBatches"] = ExamBatches };
        var dialog = await DialogService.ShowAsync<ExamBatchDetailDialog>("Sửa chi tiết đợt thi", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnExamBatchDetailUpdated.InvokeAsync();
        }
    }

    private async Task OpenDeleteDialog(ExamBatchDetailDto examBatchDetail)
    {
        var parameters = new DialogParameters 
        { 
            ["Message"] = $"Bạn có chắc chắn muốn xóa chi tiết đợt thi '{examBatchDetail.Name}'?" 
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Xác nhận xóa", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled)
        {
            await DeleteExamBatchDetail(examBatchDetail);
        }
    }

    private async Task DeleteExamBatchDetail(ExamBatchDetailDto examBatchDetail)
    {
        try
        {
            await ExamBatchDetailService.DeleteAsync(examBatchDetail.ExamBatchDetailId);
            Snackbar.Add("Xóa chi tiết đợt thi thành công", Severity.Success);
            await OnExamBatchDetailUpdated.InvokeAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }
}
