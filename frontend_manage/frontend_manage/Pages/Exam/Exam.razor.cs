using System.Collections.Generic;
using System.Linq;
using frontend_manage.DTOs;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;

namespace frontend_manage.Pages.Exam;

public partial class Exam : ComponentBase
{
    [Inject] private StudentService StudentService { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery(Name = "studentExamSessionId")]
    public int? StudentExamSessionId { get; set; }

    private bool _isLoading;
    private string? _errorMessage;
    private StartExamResponseDto? _response;
    private List<QuestionDisplayItem> _questionDisplayItems = new();
    private bool _showQuestionPreview = true;
    private int? _currentSessionId;

    private bool _canTriggerFetch => StudentExamSessionId.HasValue && !_isLoading;
    private string _fetchButtonLabel => _isLoading ? "Đang khởi tạo..." : "Tải lại dữ liệu";

    protected override async Task OnParametersSetAsync()
    {
        if (!StudentExamSessionId.HasValue)
        {
            _errorMessage = "Không xác định được ca thi. Vui lòng quay lại Dashboard và chọn lại.";
            _response = null;
            _questionDisplayItems.Clear();
            return;
        }

        if (_currentSessionId == StudentExamSessionId)
        {
            return;
        }

        await FetchExamAsync(StudentExamSessionId.Value, false);
    }

    private async Task ReloadExamAsync()
    {
        if (!StudentExamSessionId.HasValue)
        {
            _errorMessage = "Không xác định được ca thi để tải.";
            return;
        }

        await FetchExamAsync(StudentExamSessionId.Value, true);
    }

    private async Task FetchExamAsync(int studentExamSessionId, bool force)
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        _errorMessage = null;

        if (force)
        {
            _response = null;
            _questionDisplayItems.Clear();
        }

        try
        {
            var result = await StudentService.StartExamAsync(studentExamSessionId);
            if (result == null)
            {
                _errorMessage = "API không trả dữ liệu hoặc báo lỗi.";
                _questionDisplayItems.Clear();
                _response = null;
            }
            else
            {
                _response = result;
                _currentSessionId = studentExamSessionId;
                _questionDisplayItems = BuildQuestionDisplayItems(result.ExamPaper?.QuestionStructures);
                _showQuestionPreview = _questionDisplayItems.Count > 0;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Lỗi gọi API: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private static List<QuestionDisplayItem> BuildQuestionDisplayItems(IEnumerable<QuestionStructureDto>? questions, int depth = 0)
    {
        var items = new List<QuestionDisplayItem>();
        if (questions == null)
        {
            return items;
        }

        foreach (var question in questions.OrderBy(q => q.Order))
        {
            items.Add(new QuestionDisplayItem
            {
                Question = question,
                Depth = depth
            });

            if (question.ChildQuestions?.Any() == true)
            {
                items.AddRange(BuildQuestionDisplayItems(question.ChildQuestions, depth + 1));
            }
        }

        return items;
    }

    private static string GetIndentStyle(int depth) => $"margin-left: {depth * 16}px";
    private void ToggleQuestionPreviewVisibility() => _showQuestionPreview = !_showQuestionPreview;
    protected static string GetAnswerLetter(int order)
    {
        if (order <= 0)
        {
            return string.Empty;
        }

        var index = (order - 1) % 26;
        return ((char)('A' + index)).ToString();
    }

    private class QuestionDisplayItem
    {
        public QuestionStructureDto Question { get; set; } = new();
        public int Depth { get; set; }
    }
}