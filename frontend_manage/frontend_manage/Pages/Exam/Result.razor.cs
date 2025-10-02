using Microsoft.AspNetCore.Components;
using MudBlazor;
using frontend_manage.DTOs;
using frontend_manage.Services;
using System.Text.Json;

namespace frontend_manage.Pages.Exam;

public partial class Result : IDisposable
{
    [Parameter]
    [SupplyParameterFromQuery]
    public int? studentExamSessionId { get; set; }

    [Inject] private StudentService StudentService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;

    private ExamSubmissionDto? submission;
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        if (studentExamSessionId == null)
        {
            Snackbar.Add("Thiếu thông tin ca thi.", Severity.Error);
            Navigation.NavigateTo("/student-dashboard");
            return;
        }

        try
        {
            submission = await StudentService.GetSubmissionResultAsync(studentExamSessionId.Value);
            if (submission == null)
            {
                Snackbar.Add("Không tìm thấy kết quả bài thi.", Severity.Warning);
            }

            // Đăng ký nhận điểm cập nhật qua SignalR (nếu có)
            NotificationService.OnExamScoreReceived += HandleExamScoreReceived;

            // Đảm bảo đã kết nối SignalR để có thể nhận sự kiện
            if (!NotificationService.IsConnected)
            {
                _ = NotificationService.StartAsync();
            }

            // Tham gia group theo mã sinh viên để nhận điểm đẩy về
            // Ưu tiên dùng StudentCode từ submission nếu có; nếu chưa có thì lấy từ query service khác nếu cần
            var studentCode = submission?.StudentCode;
            if (!string.IsNullOrWhiteSpace(studentCode))
            {
                try
                {
                    await NotificationService.JoinStudentGroup(studentCode);
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi khi tải kết quả: {ex.Message}", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }

    private void HandleExamScoreReceived(object data)
    {
        try
        {
            if (data is JsonElement json)
            {
                // Cập nhật các trường cần thiết từ payload
                submission ??= new ExamSubmissionDto();
                if (json.TryGetProperty("studentCode", out var pStudentCode))
                    submission.StudentCode = pStudentCode.GetString() ?? submission.StudentCode;
                if (json.TryGetProperty("shuffledExamPaperId", out var pPaperId))
                    submission.ShuffledExamPaperId = pPaperId.GetInt32();
                if (json.TryGetProperty("score", out var pScore))
                    submission.Score = pScore.GetDouble();
                if (json.TryGetProperty("correctAnswers", out var pCorrect))
                    submission.CorrectAnswers = pCorrect.GetInt32();
                if (json.TryGetProperty("totalQuestions", out var pTotal))
                    submission.TotalQuestions = pTotal.GetInt32();
                if (json.TryGetProperty("endTime", out var pEnd))
                    submission.EndTime = pEnd.GetDateTime();
                if (json.TryGetProperty("studentAnswersString", out var pAns))
                    submission.StudentAnswersString = pAns.GetString() ?? submission.StudentAnswersString;
                if (json.TryGetProperty("answerKey", out var pKey))
                    submission.AnswerKey = pKey.GetString() ?? submission.AnswerKey;

                InvokeAsync(StateHasChanged);
            }
        }
        catch
        {
            // bỏ qua lỗi parse, không làm gián đoạn UI
        }
    }

    public void Dispose()
    {
        NotificationService.OnExamScoreReceived -= HandleExamScoreReceived;
        // Không có studentCode chắc chắn ở đây để Leave, bỏ qua an toàn
    }
    
    
    private string ScoreSvgDataUri => BuildScoreSvgDataUri(
        submission?.Score ?? 0);
    
    private string BuildScoreSvgDataUri(double score, int width = 560, int height = 220)
    {
        var scoreText = score.ToString("0.00");
        var midY = (int)Math.Round(height * 0.62);
        var bigFont = (int)Math.Round(height * 0.60);

        var svg = $@"
<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}'>
  <defs>
    <linearGradient id='g' x1='0' y1='0' x2='1' y2='1'>
      <stop offset='0' stop-color='#1976d2'/><stop offset='1' stop-color='#42a5f5'/>
    </linearGradient>
    <filter id='ds' x='-20%' y='-20%' width='140%' height='140%'>
      <feDropShadow dx='0' dy='8' stdDeviation='10' flood-color='#1565c0' flood-opacity='.35'/>
    </filter>
  </defs>
  <rect rx='24' width='{width}' height='{height}' fill='url(#g)' filter='url(#ds)'/>
  <text x='{width / 2}' y='{midY}' text-anchor='middle'
        font-family='Segoe UI,Roboto,Arial' font-weight='900' font-size='{bigFont}'
        fill='#ffffff'>{scoreText}</text>
</svg>";

        var bytes = System.Text.Encoding.UTF8.GetBytes(svg);
        return "data:image/svg+xml;base64," + Convert.ToBase64String(bytes);
    }



}