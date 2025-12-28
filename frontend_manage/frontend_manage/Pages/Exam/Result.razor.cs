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
                System.Diagnostics.Debug.WriteLine($"[OnInitializedAsync] Submission loaded. Score: {_submission.Score}, HasValue: {_submission.Score.HasValue}");
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
            System.Diagnostics.Debug.WriteLine($"[LoadSubmissionAsync] Submission loaded. Score: {_submission.Score}, HasValue: {_submission.Score.HasValue}, CorrectAnswers: {_submission.CorrectAnswers}, TotalQuestions: {_submission.TotalQuestions}");
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
        _submission != null && _submission.Score.HasValue
            ? $"{_submission.Score.Value:F2}"
            : "Chưa có điểm";

    private string ScoreSvgDataUri => BuildScoreSvgDataUri(
        _submission != null && _submission.Score.HasValue ? _submission.Score.Value : 0);

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

        // Debug: Log raw strings
        System.Diagnostics.Debug.WriteLine($"[BuildAnswerComparisons] StudentAnswersString: {_submission.StudentAnswersString}");
        System.Diagnostics.Debug.WriteLine($"[BuildAnswerComparisons] AnswerKey: {_submission.AnswerKey}");

        var studentAnswers = ParseAnswerString(_submission.StudentAnswersString);
        var correctAnswers = ParseAnswerString(_submission.AnswerKey);

        // Debug: Log parsed dictionaries
        System.Diagnostics.Debug.WriteLine($"[BuildAnswerComparisons] Parsed student answers count: {studentAnswers.Count}");
        foreach (var kvp in studentAnswers)
        {
            System.Diagnostics.Debug.WriteLine($"[BuildAnswerComparisons] Student Q{kvp.Key}: {kvp.Value ?? "null"}");
        }
        System.Diagnostics.Debug.WriteLine($"[BuildAnswerComparisons] Parsed correct answers count: {correctAnswers.Count}");
        foreach (var kvp in correctAnswers)
        {
            System.Diagnostics.Debug.WriteLine($"[BuildAnswerComparisons] Correct Q{kvp.Key}: {kvp.Value ?? "null"}");
        }

        // Lấy tất cả question IDs từ cả student answers và correct answers
        // Đảm bảo không bỏ sót câu hỏi nào
        var allQuestionIds = studentAnswers.Keys
            .Union(correctAnswers.Keys)
            .OrderBy(id => id)
            .ToList();

        System.Diagnostics.Debug.WriteLine($"[BuildAnswerComparisons] Total unique question IDs: {allQuestionIds.Count}");

        var list = new List<AnswerComparison>();
        foreach (var questionId in allQuestionIds)
        {
            studentAnswers.TryGetValue(questionId, out var studentAnswer);
            correctAnswers.TryGetValue(questionId, out var correctAnswer);

            var state = DetermineState(studentAnswer, correctAnswer);
            list.Add(new AnswerComparison(questionId, studentAnswer, correctAnswer, state));
            
            System.Diagnostics.Debug.WriteLine($"[BuildAnswerComparisons] Q{questionId}: Student={studentAnswer ?? "null"}, Correct={correctAnswer ?? "null"}, State={state}");
        }

        _answerComparisons = list;
        
        // Đếm lại thống kê từ danh sách comparisons
        _correctCount = list.Count(a => a.State == AnswerState.Correct);
        _incorrectCount = list.Count(a => a.State == AnswerState.Incorrect);
        _unansweredCount = list.Count(a => a.State == AnswerState.Unanswered);
        
        System.Diagnostics.Debug.WriteLine($"[BuildAnswerComparisons] Summary: Correct={_correctCount}, Incorrect={_incorrectCount}, Unanswered={_unansweredCount}");
        
        // Đảm bảo tổng số câu hỏi khớp với TotalQuestions nếu có
        var totalFromComparisons = _correctCount + _incorrectCount + _unansweredCount;
        if (_submission.TotalQuestions.HasValue && 
            totalFromComparisons != _submission.TotalQuestions.Value &&
            _submission.TotalQuestions.Value > 0)
        {
            // Nếu số lượng không khớp, có thể có câu hỏi chưa được parse
            // Log warning hoặc điều chỉnh (tùy yêu cầu)
            System.Diagnostics.Debug.WriteLine(
                $"Warning: Total questions mismatch. Expected: {_submission.TotalQuestions.Value}, " +
                $"Found in comparisons: {totalFromComparisons}");
        }
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
            System.Diagnostics.Debug.WriteLine("[ParseAnswerString] Input is null or empty");
            return result;
        }

        System.Diagnostics.Debug.WriteLine($"[ParseAnswerString] Parsing: {answers}");

        // Format backend: "(key:value);(key:value);..."
        // Backend parse: Split by ';', trim '()', split by ':', lưu Dictionary<string, string>
        var pairs = answers.Split(';', StringSplitOptions.RemoveEmptyEntries);
        System.Diagnostics.Debug.WriteLine($"[ParseAnswerString] Found {pairs.Length} segments after splitting by ';'");
        
        foreach (var rawPair in pairs)
        {
            var trimmed = rawPair.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            // Bỏ ngoặc tròn nếu có (giống backend: Trim('(', ')'))
            if (trimmed.StartsWith("(") && trimmed.EndsWith(")"))
            {
                trimmed = trimmed[1..^1].Trim();
            }

            // Split by ':' (giống backend: Split(':', 2))
            var segments = trimmed.Split(':', 2, StringSplitOptions.TrimEntries);
            if (segments.Length != 2)
            {
                System.Diagnostics.Debug.WriteLine($"[ParseAnswerString] Skipping invalid segment: {trimmed}");
                continue;
            }

            // Parse questionId (key) - backend lưu string nhưng frontend cần int
            if (!int.TryParse(segments[0], out var questionId))
            {
                System.Diagnostics.Debug.WriteLine($"[ParseAnswerString] Failed to parse questionId from: {segments[0]}");
                continue;
            }

            // Lấy value (backend lưu string, giữ nguyên)
            var value = segments[1];
            
            // Backend kiểm tra: string.IsNullOrWhiteSpace(value) || value == "-"
            if (string.IsNullOrWhiteSpace(value) || value == "-")
            {
                // Backend không lưu vào dict nếu empty hoặc "-", nhưng frontend cần biết là null
                if (!result.ContainsKey(questionId))
                {
                    result[questionId] = null;
                }
                System.Diagnostics.Debug.WriteLine($"[ParseAnswerString] Q{questionId}: null (empty or '-')");
            }
            else
            {
                // Backend ghi đè value mới (answersDict[key] = value)
                result[questionId] = value;
                System.Diagnostics.Debug.WriteLine($"[ParseAnswerString] Q{questionId}: {value}");
            }
        }

        System.Diagnostics.Debug.WriteLine($"[ParseAnswerString] Parsed {result.Count} answers");
        return result;
    }

    private static AnswerState DetermineState(string? studentAnswer, string? correctAnswer)
    {
        // Backend logic: 
        // if (string.IsNullOrWhiteSpace(studentAnswer) || studentAnswer == "-") => không đếm
        // if (studentAnswer == correctAnswer) => correct
        
        // Nếu học sinh chưa trả lời (giống backend check)
        if (string.IsNullOrWhiteSpace(studentAnswer) || studentAnswer == "-")
        {
            System.Diagnostics.Debug.WriteLine($"[DetermineState] Unanswered: studentAnswer is null/empty or '-'");
            return AnswerState.Unanswered;
        }

        // Nếu không có đáp án đúng để so sánh
        if (string.IsNullOrWhiteSpace(correctAnswer) || correctAnswer == "-")
        {
            System.Diagnostics.Debug.WriteLine($"[DetermineState] Unanswered: correctAnswer is null/empty or '-', but student answered: {studentAnswer}");
            return AnswerState.Unanswered;
        }

        // Backend so sánh: studentAnswer == correctAnswer (string comparison, exact match)
        // Không trim, không case-insensitive, so sánh trực tiếp
        if (studentAnswer == correctAnswer)
        {
            System.Diagnostics.Debug.WriteLine($"[DetermineState] Correct: '{studentAnswer}' == '{correctAnswer}'");
            return AnswerState.Correct;
        }

        System.Diagnostics.Debug.WriteLine($"[DetermineState] Incorrect: '{studentAnswer}' != '{correctAnswer}'");
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

    // Helper methods cho UI mới
    private string GetScoreClass()
    {
        if (_submission?.Score == null) return "score-none";
        var score = _submission.Score.Value;
        if (score >= 8.0) return "score-excellent";
        if (score >= 6.5) return "score-good";
        if (score >= 5.0) return "score-pass";
        return "score-fail";
    }

    private static string GetAnswerClass(AnswerState state) =>
        state switch
        {
            AnswerState.Correct => "correct",
            AnswerState.Incorrect => "incorrect",
            _ => "skipped"
        };

    private static string GetTooltipText(AnswerComparison answer, int index)
    {
        var stateText = answer.State switch
        {
            AnswerState.Correct => "✓ Đúng",
            AnswerState.Incorrect => "✗ Sai",
            _ => "○ Chưa trả lời"
        };
        return $"Câu {index}: {stateText}";
    }

    private string GetScoreProgress()
    {
        if (_submission?.Score == null) return "0";
        var score = _submission.Score.Value;
        var progress = (score / 10.0) * 283; // 283 is circumference of circle with r=45
        return progress.ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
    }

    private string GetResultMessage()
    {
        if (_submission?.Score == null) return "Chưa có điểm";
        var score = _submission.Score.Value;
        if (score >= 8.0) return "Xuất sắc! 🎉";
        if (score >= 6.5) return "Tốt lắm! 👍";
        if (score >= 5.0) return "Đạt yêu cầu ✓";
        return "Cần cố gắng hơn";
    }

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
                // Khởi tạo _submission nếu chưa có
                _submission ??= new ExamSubmissionDto();
                
                // Cập nhật các thông tin cơ bản
                if (json.TryGetProperty("score", out var pScore))
                    _submission.Score = pScore.GetDouble();
                if (json.TryGetProperty("correctAnswers", out var pCorrect))
                    _submission.CorrectAnswers = pCorrect.GetInt32();
                if (json.TryGetProperty("totalQuestions", out var pTotal))
                    _submission.TotalQuestions = pTotal.GetInt32();

                // Cập nhật thời gian
                if (json.TryGetProperty("startTime", out var pStart) && pStart.ValueKind != JsonValueKind.Null)
                {
                    if (pStart.TryGetDateTime(out var start))
                        _submission.StartTime = start;
                }

                if (json.TryGetProperty("endTime", out var pEnd) && pEnd.ValueKind != JsonValueKind.Null)
                {
                    if (pEnd.TryGetDateTime(out var end))
                        _submission.EndTime = end;
                }

                // Cập nhật đáp án để BuildAnswerComparisons có dữ liệu mới nhất
                if (json.TryGetProperty("studentAnswersString", out var pStudentAnswers) && pStudentAnswers.ValueKind != JsonValueKind.Null)
                    _submission.StudentAnswersString = pStudentAnswers.GetString();
                
                if (json.TryGetProperty("answerKey", out var pAnswerKey) && pAnswerKey.ValueKind != JsonValueKind.Null)
                    _submission.AnswerKey = pAnswerKey.GetString();

                // Build lại comparisons để cập nhật các chấm tròn và thống kê chi tiết
                BuildAnswerComparisons();

                InvokeAsync(StateHasChanged);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HandleExamScoreReceived] Error: {ex.Message}");
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

