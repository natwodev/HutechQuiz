using System;
using System.Collections.Generic;
using System.Linq;
using frontend_manage.DTOs;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace frontend_manage.Pages.Exam;

public partial class Exam : ComponentBase, IAsyncDisposable
{
    [Inject] private StudentService StudentService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery(Name = "studentExamSessionId")]
    public int? StudentExamSessionId { get; set; }

    private bool _isLoading;
    private string? _errorMessage;
    private StartExamResponseDto? _response;
    private List<QuestionDisplayItem> _questionDisplayItems = new();
    private int? _currentSessionId;
    private readonly Dictionary<int, object?> _questionAnswers = new();
    private readonly List<NavigationItem> _navigationItems = new();
    private int _activeQuestionIndex;
    private bool _isFullscreen;
    private bool _autoFullscreenAttempted;
    private readonly Dictionary<int, string> _questionLabelMap = new();
    private bool _showFullscreenWarning;
    private bool _showFocusWarning;
    private DotNetObjectReference<Exam>? _dotNetRef;

    private bool _canTriggerFetch => StudentExamSessionId.HasValue && !_isLoading;
    private string _fetchButtonLabel => _isLoading ? "Đang khởi tạo..." : "Tải lại dữ liệu";
    private QuestionDisplayItem? ActiveQuestion =>
        _activeQuestionIndex >= 0 && _activeQuestionIndex < _questionDisplayItems.Count
            ? _questionDisplayItems[_activeQuestionIndex]
            : null;

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_autoFullscreenAttempted)
        {
            _autoFullscreenAttempted = true;
            _dotNetRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("examFullscreen.registerExamEvents", _dotNetRef);
            var entered = await TryEnterFullscreenAsync();
            if (entered)
            {
                StateHasChanged();
            }
        }
    }

    [JSInvokable]
    public Task OnFullscreenStateChanged(bool isFullscreen)
    {
        _isFullscreen = isFullscreen;
        if (!_isFullscreen && _autoFullscreenAttempted)
        {
            _showFullscreenWarning = true;
        }
        else if (_isFullscreen)
        {
            _showFullscreenWarning = false;
        }

        InvokeAsync(StateHasChanged);
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnVisibilityChanged(bool hidden)
    {
        if (hidden)
        {
            _showFocusWarning = true;
            InvokeAsync(StateHasChanged);
        }

        return Task.CompletedTask;
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
            _navigationItems.Clear();
            _questionLabelMap.Clear();
            _questionAnswers.Clear();
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
                _navigationItems.Clear();
                _questionLabelMap.Clear();
                _navigationItems.AddRange(BuildNavigationItems());
                _activeQuestionIndex = 0;
                _questionAnswers.Clear();
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

    private static List<QuestionDisplayItem> BuildQuestionDisplayItems(IEnumerable<QuestionStructureDto>? questions)
    {
        if (questions == null)
        {
            return new List<QuestionDisplayItem>();
        }

        return questions
            .OrderBy(q => q.Order)
            .Select(q => new QuestionDisplayItem
            {
                Question = q,
                Depth = 0
            })
            .ToList();
    }

    private List<NavigationItem> BuildNavigationItems()
    {
        var items = new List<NavigationItem>();
        var counter = 1;
        for (var idx = 0; idx < _questionDisplayItems.Count; idx++)
        {
            CollectNavigationLeaves(_questionDisplayItems[idx].Question, idx, items, ref counter);
        }

        return items;
    }

    private void CollectNavigationLeaves(QuestionStructureDto question, int parentIndex, List<NavigationItem> items, ref int counter)
    {
        if (question.ChildQuestions?.Any() == true)
        {
            foreach (var child in question.ChildQuestions.OrderBy(q => q.Order))
            {
                CollectNavigationLeaves(child, parentIndex, items, ref counter);
            }

            return;
        }

        var label = counter.ToString();
        _questionLabelMap[question.OriginalExamPaperDetailId] = label;
        counter++;

        items.Add(new NavigationItem
        {
            QuestionId = question.OriginalExamPaperDetailId,
            ParentIndex = parentIndex
        });
    }

    private string GetQuestionLabel(int questionId) =>
        _questionLabelMap.TryGetValue(questionId, out var label) ? label : string.Empty;

    public async ValueTask DisposeAsync()
    {
        if (_dotNetRef != null)
        {
            await JS.InvokeVoidAsync("examFullscreen.unregisterExamEvents");
            _dotNetRef.Dispose();
        }
    }
 
    private int? GetSelectedAnswerId(int questionId)
    {
        if (_questionAnswers.TryGetValue(questionId, out var stored) && stored is int answer)
        {
            return answer;
        }

        return null;
    }

    private Task HandleQuestionAnswered((int questionId, object? value) payload)
    {
        _questionAnswers[payload.questionId] = payload.value;
        return Task.CompletedTask;
    }

    private void GoToQuestion(int index)
    {
        if (index < 0 || index >= _questionDisplayItems.Count)
        {
            return;
        }

        _activeQuestionIndex = index;
    }

    private void NextQuestion() => GoToQuestion(_activeQuestionIndex + 1);
    private void PreviousQuestion() => GoToQuestion(_activeQuestionIndex - 1);
    private void NavigateToEntry(int entryIndex)
    {
        if (entryIndex < 0 || entryIndex >= _navigationItems.Count)
        {
            return;
        }

        GoToQuestion(_navigationItems[entryIndex].ParentIndex);
    }

    private async Task ToggleFullscreenAsync()
    {
        try
        {
            var isActive = await JS.InvokeAsync<bool>("examFullscreen.isActive");
            if (!isActive)
            {
                await TryEnterFullscreenAsync();
            }
            else
            {
                await JS.InvokeVoidAsync("examFullscreen.exit");
                _isFullscreen = false;
            }
        }
        catch
        {
            _isFullscreen = false;
        }
    }

    private async Task<bool> TryEnterFullscreenAsync()
    {
        try
        {
            var entered = await JS.InvokeAsync<bool>("examFullscreen.enter", "#exam-shell");
            if (entered)
            {
                _isFullscreen = true;
                return true;
            }
        }
        catch
        {
            _isFullscreen = false;
        }

        return false;
    }

    private class QuestionDisplayItem
    {
        public QuestionStructureDto Question { get; set; } = new();
        public int Depth { get; set; }
    }

    private class NavigationItem
    {
        public int QuestionId { get; set; }
        public int ParentIndex { get; set; }
    }

}