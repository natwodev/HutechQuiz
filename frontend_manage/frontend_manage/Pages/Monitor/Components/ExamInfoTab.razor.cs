using Microsoft.AspNetCore.Components;
using MudBlazor;
using frontend_manage.Services;
using frontend_manage.DTOs;

namespace frontend_manage.Pages.Monitor.Components;

public partial class ExamInfoTab : ComponentBase
{
    [Inject] private MonitorApi MonitorApi { get; set; } = default!;

    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Parameter] public SubjectExamRoomStatusDto? Subject { get; set; }

    [Parameter] public int? ExamSessionSubjectId { get; set; }

    private string GetRemainingTime()
    {
        if (Subject == null) return "00:00:00";

        var now = DateTime.Now;
        var endTime = Subject.ExamSessionEndTime;
        var remaining = endTime - now;

        if (remaining.TotalSeconds <= 0) return "00:00:00";

        return $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
    }

    private double GetProgressValue()
    {
        if (Subject == null) return 0;

        var now = DateTime.Now;
        var startTime = Subject.ExamSessionStartTime;
        var endTime = Subject.ExamSessionEndTime;
        var totalDuration = endTime - startTime;
        var elapsed = now - startTime;

        if (totalDuration.TotalSeconds <= 0) return 0;
        if (elapsed.TotalSeconds <= 0) return 0;
        if (elapsed.TotalSeconds >= totalDuration.TotalSeconds) return 100;

        return (elapsed.TotalSeconds / totalDuration.TotalSeconds) * 100;
    }

    private void EndExamEarly()
    {
        // Logic to end exam early
    }

    private void ExtendTime()
    {
        // Logic to extend exam time
    }

    private async Task OpenExam()
    {
        if (ExamSessionSubjectId == null)
        {
            Snackbar.Add("Không thể xác định ca thi", Severity.Error);
            return;
        }

        try
        {
            var result = await MonitorApi.UpdateIsActiveAsync(ExamSessionSubjectId.Value, true);
            Snackbar.Add(result, Severity.Success);

            // Cập nhật giao diện ngay lập tức
            if (Subject != null)
            {
                Subject.IsActive = true;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi khi mở ca thi: {ex.Message}", Severity.Error);
        }
    }

    private async Task CloseExam()
    {
        if (ExamSessionSubjectId == null)
        {
            Snackbar.Add("Không thể xác định ca thi", Severity.Error);
            return;
        }

        try
        {
            var result = await MonitorApi.UpdateIsActiveAsync(ExamSessionSubjectId.Value, false);
            Snackbar.Add(result, Severity.Success);

            // Cập nhật giao diện ngay lập tức
            if (Subject != null)
            {
                Subject.IsActive = false;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi khi tắt ca thi: {ex.Message}", Severity.Error);
        }
    }
}