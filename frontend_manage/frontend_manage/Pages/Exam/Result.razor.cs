using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Exam;

public partial class Result : ComponentBase, IDisposable
{
    [Inject] private StudentService StudentService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery(Name = "studentExamSessionId")]
    public int? StudentExamSessionId { get; set; }

    private bool _isLoading;
    private string? _errorMessage;
    private ExamSubmissionDto? _submission;
    private int? _loadedSessionId;
    private List<AnswerComparison> _answerComparisons = new List<AnswerComparison>();
    private int _correctCount;
    private int _incorrectCount;
    private int _unansweredCount;

    protected override async Task OnInitializedAsync()
    {
        if (!StudentExamSessionId.HasValue)
        {
            _errorMessage = "Thiếu tham số studentExamSessionId. Vui lòng quay lại trang thi.";
            _submission = null;
            _isLoading = false;
            return;
        }

        try
        {
            _submission = await StudentService.GetSubmissionResultAsync(StudentExamSessionId.Value);
            if (_submission == null)
            {
                _errorMessage = "Không tìm thấy kết quả bài thi.";
                Snackbar.Add(_errorMessage, Severity.Warning);
            }
            else
            {
                _loadedSessionId = StudentExamSessionId.Value;
                BuildAnswerComparisons();
            }

            // Đăng ký nhận điểm cập nhật qua SignalR (nếu có)
            NotificationService.OnExamScoreReceived += HandleExamScoreReceived;

            // Đảm bảo đã kết nối SignalR để có thể nhận sự kiện
            if (!NotificationService.IsConnected)
            {
                _ = NotificationService.StartAsync();
            }

            // Tham gia group theo mã sinh viên để nhận điểm đẩy về
            var studentCode = _submission?.StudentCode;
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
            _errorMessage = $"Lỗi khi tải kết quả: {ex.Message}";
            Snackbar.Add(_errorMessage, Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!StudentExamSessionId.HasValue)
        {
            return;
        }

        if (_loadedSessionId == StudentExamSessionId && _submission != null)
        {
            return;
        }

        await LoadSubmissionAsync(StudentExamSessionId.Value);
    }

    private async Task LoadSubmissionAsync(int sessionId)
    {
        _isLoading = true;
        _errorMessage = null;

        var result = await StudentService.GetSubmissionResultAsync(sessionId);
        if (result == null)
        {
            _errorMessage = "Không tìm thấy kết quả nộp bài cho ca thi này.";
            _submission = null;
            _answerComparisons = new List<AnswerComparison>();
            ResetSummaryCounts();
            Snackbar.Add(_errorMessage, Severity.Warning);
        }
        else
        {
            _submission = result;
            _loadedSessionId = sessionId;
            BuildAnswerComparisons();

            // Tham gia group theo mã sinh viên để nhận điểm đẩy về
            var studentCode = _submission?.StudentCode;
            if (!string.IsNullOrWhiteSpace(studentCode))
            {
                try
                {
                    // Đảm bảo SignalR connection đã sẵn sàng
                    if (!NotificationService.IsConnected)
                    {
                        _ = NotificationService.StartAsync();
                    }
                    await NotificationService.JoinStudentGroup(studentCode);
                }
                catch { }
            }
        }

        _isLoading = false;
        StateHasChanged();
    }

    private string ScoreDisplay =>
        _submission?.Score.HasValue == true
            ? $"{_submission.Score:0.##}"
            : "Chưa có điểm";

    private string ScoreSvgDataUri => BuildScoreSvgDataUri(
        _submission?.Score ?? 0);

    private static string FormatDate(DateTime? value) =>
        value?.ToString("HH:mm dd/MM/yyyy") ?? "Không có";

    private static string FormatDuration(DateTime? start, DateTime? end)
    {
        if (!start.HasValue || !end.HasValue)
        {
            return "Không xác định";
        }

        var duration = end.Value - start.Value;
        if (duration.TotalSeconds < 0)
        {
            return "Không hợp lệ";
        }

        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}h {duration.Minutes}m {duration.Seconds}s"
            : $"{duration.Minutes}m {duration.Seconds}s";
    }

    private void BuildAnswerComparisons()
    {
        if (_submission == null)
        {
            _answerComparisons = new List<AnswerComparison>();
            ResetSummaryCounts();
            return;
        }

        var studentAnswers = ParseAnswerString(_submission.StudentAnswersString);
        var correctAnswers = ParseAnswerString(_submission.AnswerKey);

        var allQuestionIds = studentAnswers.Keys
            .Union(correctAnswers.Keys)
            .OrderBy(id => id);

        var list = new List<AnswerComparison>();
        foreach (var questionId in allQuestionIds)
        {
            studentAnswers.TryGetValue(questionId, out var studentAnswer);
            correctAnswers.TryGetValue(questionId, out var correctAnswer);

            var state = DetermineState(studentAnswer, correctAnswer);
            list.Add(new AnswerComparison(questionId, studentAnswer, correctAnswer, state));
        }

        _answerComparisons = list;
        _correctCount = list.Count(a => a.State == AnswerState.Correct);
        _incorrectCount = list.Count(a => a.State == AnswerState.Incorrect);
        _unansweredCount = list.Count(a => a.State == AnswerState.Unanswered);
    }

    private void ResetSummaryCounts()
    {
        _correctCount = 0;
        _incorrectCount = 0;
        _unansweredCount = 0;
    }

    private static Dictionary<int, string?> ParseAnswerString(string? answers)
    {
        var result = new Dictionary<int, string?>();
        if (string.IsNullOrWhiteSpace(answers))
        {
            return result;
        }

        var pairs = answers.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var rawPair in pairs)
        {
            var trimmed = rawPair.Trim();
            if (trimmed.StartsWith("(") && trimmed.EndsWith(")"))
            {
                trimmed = trimmed[1..^1];
            }

            var segments = trimmed.Split(':', 2, StringSplitOptions.TrimEntries);
            if (segments.Length != 2)
            {
                continue;
            }

            if (!int.TryParse(segments[0], out var questionId))
            {
                continue;
            }

            var value = segments[1];
            result[questionId] = string.IsNullOrWhiteSpace(value) || value == "-"
                ? null
                : value;
        }

        return result;
    }

    private static AnswerState DetermineState(string? studentAnswer, string? correctAnswer)
    {
        if (string.IsNullOrEmpty(studentAnswer))
        {
            return AnswerState.Unanswered;
        }

        if (!string.IsNullOrEmpty(correctAnswer) &&
            string.Equals(studentAnswer, correctAnswer, StringComparison.OrdinalIgnoreCase))
        {
            return AnswerState.Correct;
        }

        return AnswerState.Incorrect;
    }

    private static Color GetStateColor(AnswerState state) =>
        state switch
        {
            AnswerState.Correct => Color.Success,
            AnswerState.Unanswered => Color.Warning,
            _ => Color.Error
        };

    private static string GetStateLabel(AnswerState state) =>
        state switch
        {
            AnswerState.Correct => "Đúng",
            AnswerState.Incorrect => "Sai",
            _ => "Chưa chọn"
        };

    private static string FormatAnswerValue(string? value) => value ?? "-";

    private enum AnswerState
    {
        Correct,
        Incorrect,
        Unanswered
    }

    private sealed record AnswerComparison(int QuestionId, string? StudentAnswer, string? CorrectAnswer, AnswerState State);

    private void HandleExamScoreReceived(object data)
    {
        try
        {
            if (data is JsonElement json)
            {
                // Chỉ cập nhật điểm số và số câu đúng từ payload
                // Không cập nhật studentAnswersString, answerKey để tránh làm sai thống kê
                _submission ??= new ExamSubmissionDto();
                
                if (json.TryGetProperty("score", out var pScore))
                    _submission.Score = pScore.GetDouble();
                if (json.TryGetProperty("correctAnswers", out var pCorrect))
                    _submission.CorrectAnswers = pCorrect.GetInt32();
                if (json.TryGetProperty("totalQuestions", out var pTotal))
                    _submission.TotalQuestions = pTotal.GetInt32();

                // Không gọi BuildAnswerComparisons() để giữ nguyên thống kê hiện tại
                // Chỉ cập nhật điểm số và số câu đúng

                InvokeAsync(StateHasChanged);
            }
        }
        catch
        {
            // bỏ qua lỗi parse, không làm gián đoạn UI
        }
    }

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

    public void Dispose()
    {
        NotificationService.OnExamScoreReceived -= HandleExamScoreReceived;
        // Không có studentCode chắc chắn ở đây để Leave, bỏ qua an toàn
    }
}

