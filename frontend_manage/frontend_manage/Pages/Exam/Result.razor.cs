using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using frontend_manage.DTOs;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Exam;

public partial class Result : ComponentBase
{
    [Inject] private StudentService StudentService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

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

    protected override async Task OnParametersSetAsync()
    {
        if (!StudentExamSessionId.HasValue)
        {
            _errorMessage = "Thiếu tham số studentExamSessionId. Vui lòng quay lại trang thi.";
            _submission = null;
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
        }

        _isLoading = false;
        StateHasChanged();
    }

    private string ScoreDisplay =>
        _submission?.Score.HasValue == true
            ? $"{_submission.Score:0.##}"
            : "Chưa có điểm";

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
}

