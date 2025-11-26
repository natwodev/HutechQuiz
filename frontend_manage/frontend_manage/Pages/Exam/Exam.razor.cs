using System;
using System.Collections.Generic;
using System.Linq;
using frontend_manage.DTOs;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace frontend_manage.Pages.Exam;

public partial class Exam : ComponentBase, IAsyncDisposable
{
    [Inject] private StudentService StudentService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery(Name = "studentExamSessionId")]
    public int? StudentExamSessionId { get; set; }

    private bool _isLoading;
    private StartExamResponseDto? _response;
    private List<QuestionDisplayItem> _questionDisplayItems = new();
    private int? _currentSessionId;
    private readonly Dictionary<int, object?> _questionAnswers = new();
    private readonly List<NavigationItem> _navigationItems = new();
    private int _activeQuestionIndex;
    private bool _isFullscreen;
    private bool _autoFullscreenAttempted;
    private bool _jsEventsRegistered;
    private readonly Dictionary<int, string> _questionLabelMap = new();
    private DotNetObjectReference<Exam>? _dotNetRef;

    // Fullscreen chỉ được bật khi AllowViewMaterialsShuffled == false
    private bool IsFullscreenEnabled => !(_response?.ExamPaper?.AllowViewMaterials ?? true);

    private bool _canTriggerFetch => StudentExamSessionId.HasValue && !_isLoading;
    private string _fetchButtonLabel => _isLoading ? "Đang khởi tạo..." : "Tải lại dữ liệu";
    private QuestionDisplayItem? ActiveQuestion =>
        _activeQuestionIndex >= 0 && _activeQuestionIndex < _questionDisplayItems.Count
            ? _questionDisplayItems[_activeQuestionIndex]
            : null;
    private StudentExamSessionCacheDto? StudentSession => _response?.StudentSession;
    private bool _showTimer = true;
    private bool ShouldShowTimer => _response != null && StudentSession != null && _showTimer;

    protected override async Task OnParametersSetAsync()
    {
        if (!StudentExamSessionId.HasValue)
        {
            Snackbar.Add("Thiếu tham số studentExamSessionId trong URL. Vui lòng quay lại trang Dashboard.", Severity.Warning);
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
        if (firstRender && !_jsEventsRegistered)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("examFullscreen.registerExamEvents", _dotNetRef);
            _jsEventsRegistered = true;
        }

        // Tự động bật fullscreen một lần, sau khi:
        // - JS events đã đăng ký
        // - Dữ liệu đề thi (_response) đã load
        // - AllowViewMaterialsShuffled == false (IsFullscreenEnabled == true)
        if (!_autoFullscreenAttempted && _jsEventsRegistered && IsFullscreenEnabled)
        {
            _autoFullscreenAttempted = true;
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
            Snackbar.Add("Bạn vừa thoát khỏi chế độ toàn màn hình. Vui lòng bật lại để tiếp tục làm bài thi.", Severity.Warning);
        }

        InvokeAsync(StateHasChanged);
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnVisibilityChanged(bool hidden)
    {
        if (hidden)
        {
            Snackbar.Add("Hệ thống ghi nhận bạn đã rời khỏi tab thi. Vui lòng tập trung vào bài làm.", Severity.Warning);
        }

        return Task.CompletedTask;
    }

    private async Task ReloadExamAsync()
    {
        if (!StudentExamSessionId.HasValue)
        {
            Snackbar.Add("Không xác định được ca thi để tải.", Severity.Error);
            return;
        }

        await FetchExamAsync(StudentExamSessionId.Value, true);
    }

    private Task HandleExamTimerExpired()
    {
        Snackbar.Add("Thời gian làm bài đã kết thúc.", Severity.Error);
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task FetchExamAsync(int studentExamSessionId, bool force)
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;

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
                Snackbar.Add("API không trả dữ liệu hoặc báo lỗi.", Severity.Error);
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
            Snackbar.Add($"Lỗi gọi API: {ex.Message}", Severity.Error);
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

    private async Task HandleQuestionAnswered((int questionId, object? value) payload)
    {
        // Lưu trạng thái chọn đáp án trên UI
        _questionAnswers[payload.questionId] = payload.value;

        // Nếu chưa có session id thì không gọi API
        if (!StudentExamSessionId.HasValue)
        {
            return;
        }

        // Giá trị từ UI: với câu hỏi 1 đáp án sẽ là int (answerId)
        int? answerId = null;
        if (payload.value is int intValue)
        {
            answerId = intValue;
        }

        var request = new SaveAnswerDto
        {
            StudentExamSessionId = StudentExamSessionId.Value,
            // key chính là OriginalExamPaperDetailId
            key = payload.questionId,
            // value là answerId (có thể null nếu bỏ chọn)
            value = answerId
        };

        var result = await StudentService.SaveAnswerAsync(request);

        if (result == null)
        {
            Snackbar.Add("Không thể lưu câu trả lời. Vui lòng kiểm tra kết nối.", Severity.Error);
            return;
        }

        if (!result.Success)
        {
            if (result.IsRateLimited)
            {
                Snackbar.Add(result.Message, Severity.Warning);
            }
            else if (result.IsUnauthorized)
            {
                Snackbar.Add(result.Message, Severity.Error);
            }
            else
            {
                Snackbar.Add(string.IsNullOrWhiteSpace(result.Message)
                        ? "Lưu câu trả lời thất bại."
                        : result.Message,
                    Severity.Error);
            }
        }
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
        // Không cho bật fullscreen nếu AllowViewMaterialsShuffled != false
        if (!IsFullscreenEnabled)
        {
            return;
        }

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

    private void ToggleTimerVisibility()
    {
        _showTimer = !_showTimer;
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