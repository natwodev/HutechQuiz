using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.Admin;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class ExamSessionDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public ExamSessionDto? ExamSession { get; set; }
    [Parameter] public List<ExamBatchDetailDto> ExamBatchDetails { get; set; } = new();
    
    [Inject] private AdminExamSessionService ExamSessionService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    
    private ExamSessionCreateDto examSession = new();
    private bool isEdit = false;
    private string startTimeString = "";
    private string endTimeString = "";

    protected override void OnInitialized()
    {
        if (ExamSession != null)
        {
            isEdit = true;
            examSession.Name = ExamSession.Name;
            examSession.StartTime = ExamSession.StartTime;
            examSession.EndTime = ExamSession.EndTime;
            examSession.ExamBatchDetailId = ExamSession.ExamBatchDetailId;
            
            startTimeString = ExamSession.StartTime.ToString("yyyy-MM-ddTHH:mm");
            endTimeString = ExamSession.EndTime.ToString("yyyy-MM-ddTHH:mm");
        }
        else
        {
            var now = DateTime.Now;
            startTimeString = now.ToString("yyyy-MM-ddTHH:mm");
            endTimeString = now.AddHours(2).ToString("yyyy-MM-ddTHH:mm");

            if (ExamBatchDetails.Count > 0)
            {
                examSession.ExamBatchDetailId = ExamBatchDetails.First().ExamBatchDetailId;
            }
        }
    }

    private async Task HandleSubmit()
    {
        try
        {
            if (examSession.ExamBatchDetailId == 0)
            {
                Snackbar.Add("Vui lòng chọn chi tiết đợt thi", Severity.Error);
                return;
            }

            if (DateTime.TryParse(startTimeString, out var startTime))
            {
                examSession.StartTime = startTime;
            }

            if (DateTime.TryParse(endTimeString, out var endTime))
            {
                examSession.EndTime = endTime;
            }

            if (examSession.EndTime <= examSession.StartTime)
            {
                Snackbar.Add("Thời gian kết thúc phải lớn hơn thời gian bắt đầu", Severity.Error);
                return;
            }
            
            if (isEdit && ExamSession != null)
            {
                var updateDto = new ExamSessionUpdateDto 
                { 
                    Name = examSession.Name,
                    StartTime = examSession.StartTime,
                    EndTime = examSession.EndTime,
                    ExamBatchDetailId = examSession.ExamBatchDetailId
                };
                await ExamSessionService.UpdateAsync(ExamSession.ExamSessionId, updateDto);
                Snackbar.Add("Cập nhật ca thi thành công", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                await ExamSessionService.CreateAsync(examSession);
                Snackbar.Add("Thêm ca thi thành công", Severity.Success);
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

