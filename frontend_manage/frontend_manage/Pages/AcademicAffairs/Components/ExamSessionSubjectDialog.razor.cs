using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class ExamSessionSubjectDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public ExamSessionSubjectDto? ExamSessionSubject { get; set; }
    
    [Inject] private ExamSessionSubjectService ExamSessionSubjectService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    
    private ExamSessionSubjectUpdateDto examSessionSubject = new();
    private string startTimeString = "";
    private string endTimeString = "";

    protected override void OnInitialized()
    {
        if (ExamSessionSubject != null)
        {
            examSessionSubject.ExamSessionDepartmentId = ExamSessionSubject.ExamSessionDepartmentId;
            examSessionSubject.SubjectId = ExamSessionSubject.SubjectId;
            examSessionSubject.Duration = ExamSessionSubject.Duration;
            examSessionSubject.OriginalExamPaperId = ExamSessionSubject.OriginalExamPaperId;
            examSessionSubject.IsCompleted = ExamSessionSubject.IsCompleted;
            examSessionSubject.StartTime = ExamSessionSubject.StartTime;
            examSessionSubject.EndTime = ExamSessionSubject.EndTime;
            examSessionSubject.ExamSessionSubjectCore = ExamSessionSubject.ExamSessionSubjectCore;
            
            startTimeString = ExamSessionSubject.StartTime.ToString("yyyy-MM-ddTHH:mm");
            if (ExamSessionSubject.EndTime.HasValue)
            {
                endTimeString = ExamSessionSubject.EndTime.Value.ToString("yyyy-MM-ddTHH:mm");
            }
        }
    }

    private async Task HandleSubmit()
    {
        try
        {
            // Parse datetime strings
            if (DateTime.TryParse(startTimeString, out var startTime))
            {
                examSessionSubject.StartTime = startTime;
            }
            if (!string.IsNullOrEmpty(endTimeString) && DateTime.TryParse(endTimeString, out var endTime))
            {
                examSessionSubject.EndTime = endTime;
            }
            else
            {
                examSessionSubject.EndTime = null;
            }
            
            if (ExamSessionSubject != null)
            {
                await ExamSessionSubjectService.UpdateAsync(ExamSessionSubject.ExamSessionSubjectId, examSessionSubject);
                Snackbar.Add("Cập nhật ca thi môn học thành công", Severity.Success);
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

