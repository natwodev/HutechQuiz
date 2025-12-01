using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.Admin;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class ExamSessionTab : ComponentBase
{
    [Inject] private AdminExamSessionService ExamSessionService { get; set; } = default!;
    [Inject] private AdminExamBatchDetailService ExamBatchDetailService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    [Parameter]
    public List<ExamSessionDto> ExamSessions { get; set; } = new();
    
    [Parameter]
    public List<ExamBatchDto> ExamBatches { get; set; } = new();
    
    [Parameter]
    public EventCallback OnExamSessionUpdated { get; set; }
    
    private string searchString = "";
    private List<ExamBatchDetailDto> _examBatchDetails = new();

    protected override async Task OnInitializedAsync()
    {
        _examBatchDetails = await ExamBatchDetailService.GetAllAsync();
    }

    private bool FilterFunc(ExamSessionDto examSession)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;
        
        return examSession.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
               examSession.ExamBatchDetailName?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true;
    }

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters { ["ExamSession"] = (ExamSessionDto?)null, ["ExamBatchDetails"] = _examBatchDetails };
        var dialog = await DialogService.ShowAsync<ExamSessionDialog>("Thêm ca thi mới", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnExamSessionUpdated.InvokeAsync();
        }
    }

    private async Task OpenEditDialog(ExamSessionDto examSession)
    {
        var parameters = new DialogParameters { ["ExamSession"] = examSession, ["ExamBatchDetails"] = _examBatchDetails };
        var dialog = await DialogService.ShowAsync<ExamSessionDialog>("Sửa ca thi", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnExamSessionUpdated.InvokeAsync();
        }
    }

    private async Task OpenDeleteDialog(ExamSessionDto examSession)
    {
        var parameters = new DialogParameters 
        { 
            ["Message"] = $"Bạn có chắc chắn muốn xóa ca thi '{examSession.Name}'?" 
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Xác nhận xóa", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled)
        {
            await DeleteExamSession(examSession);
        }
    }

    private async Task DeleteExamSession(ExamSessionDto examSession)
    {
        try
        {
            await ExamSessionService.DeleteAsync(examSession.ExamSessionId);
            Snackbar.Add("Xóa ca thi thành công", Severity.Success);
            await OnExamSessionUpdated.InvokeAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }
}

