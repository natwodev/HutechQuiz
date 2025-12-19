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
    [Inject] private StudentActivityService StudentActivityService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Inject] NavigationManager Navigation { get; set; } = default!;

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
    private bool _autoNext = false; // Tự động chuyển câu khi chọn đáp án
    
    // Đếm số lần vi phạm để tự động nộp bài sau 3 lần
    private int _violationCount = 0;
    private const int MAX_VIOLATIONS = 3;
    private readonly HashSet<string> _violationTypes = new()
    {
        "TabSwitch", "FullscreenExit", "Copy", "Paste", "RightClick", "DevTools", "Screenshot"
    };

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
    public async Task OnFullscreenStateChanged(bool isFullscreen)
    {
        var previousFullscreen = _isFullscreen;
        _isFullscreen = isFullscreen;
        
        // Không cảnh báo nếu cho phép xem tài liệu
        var allowViewMaterials = _response?.ExamPaper?.AllowViewMaterials ?? true;
        if (!_isFullscreen && _autoFullscreenAttempted && !allowViewMaterials)
        {
            // Ghi nhận hoạt động thoát fullscreen
            if (previousFullscreen && StudentExamSessionId.HasValue && StudentSession != null)
            {
                await HandleViolationAsync("FullscreenExit", "Bạn vừa thoát khỏi chế độ toàn màn hình. Vui lòng bật lại để tiếp tục làm bài thi.");
            }
        }

        InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public async Task OnVisibilityChanged(bool hidden)
    {
        // Luôn ghi nhận hoạt động để theo dõi, không phụ thuộc vào allowViewMaterials
        if (!StudentExamSessionId.HasValue || StudentSession == null)
    {
            Console.WriteLine($"[OnVisibilityChanged] Missing StudentExamSessionId or StudentSession. Hidden: {hidden}");
            return;
        }

        // Không cảnh báo nếu cho phép xem tài liệu
        var allowViewMaterials = _response?.ExamPaper?.AllowViewMaterials ?? true;
        
        if (hidden)
        {
            // Ghi nhận hoạt động chuyển tab (luôn ghi nhận để theo dõi)
            await RecordActivityAsync("TabSwitch", "Rời khỏi tab thi");
            
            // Chỉ cảnh báo và đếm vi phạm nếu không cho phép xem tài liệu
            if (!allowViewMaterials)
        {
                await HandleViolationAsync("TabSwitch", "Hệ thống ghi nhận bạn đã rời khỏi tab thi. Vui lòng tập trung vào bài làm.");
            }
        }
        else
        {
            // Ghi nhận khi quay lại tab (chỉ để theo dõi, không cảnh báo)
            await RecordActivityAsync("TabReturn", "Quay lại tab thi");
        }
    }

    [JSInvokable]
    public async Task OnCopyDetected()
    {
        var allowViewMaterials = _response?.ExamPaper?.AllowViewMaterials ?? true;
        if (!allowViewMaterials && StudentExamSessionId.HasValue && StudentSession != null)
        {
            await HandleViolationAsync("Copy", "Hệ thống phát hiện bạn đã sao chép nội dung. Vui lòng không sao chép trong lúc thi.");
        }
    }

    [JSInvokable]
    public async Task OnPasteDetected()
    {
        var allowViewMaterials = _response?.ExamPaper?.AllowViewMaterials ?? true;
        if (!allowViewMaterials && StudentExamSessionId.HasValue && StudentSession != null)
        {
            await HandleViolationAsync("Paste", "Hệ thống phát hiện bạn đã dán nội dung. Vui lòng không dán trong lúc thi.");
        }
    }

    [JSInvokable]
    public async Task OnRightClickDetected()
    {
        var allowViewMaterials = _response?.ExamPaper?.AllowViewMaterials ?? true;
        if (!allowViewMaterials && StudentExamSessionId.HasValue && StudentSession != null)
        {
            await HandleViolationAsync("RightClick", "Hệ thống phát hiện bạn đã click chuột phải. Vui lòng không sử dụng menu chuột phải trong lúc thi.");
        }
    }

    [JSInvokable]
    public async Task OnDevToolsDetected()
    {
        var allowViewMaterials = _response?.ExamPaper?.AllowViewMaterials ?? true;
        if (!allowViewMaterials && StudentExamSessionId.HasValue && StudentSession != null)
        {
            await HandleViolationAsync("DevTools", "Hệ thống phát hiện bạn đã mở DevTools. Vui lòng đóng DevTools để tiếp tục làm bài thi.");
        }
    }

    [JSInvokable]
    public async Task OnScreenshotDetected(string key)
    {
        var allowViewMaterials = _response?.ExamPaper?.AllowViewMaterials ?? true;
        if (!allowViewMaterials && StudentExamSessionId.HasValue && StudentSession != null)
        {
            await HandleViolationAsync("Screenshot", $"Hệ thống phát hiện bạn đã chụp màn hình. Vui lòng không chụp màn hình trong lúc thi.");
        }
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

                // Khởi tạo lại map câu trả lời
                _questionAnswers.Clear();
                
                // Reset bộ đếm vi phạm khi tải lại bài thi
                _violationCount = 0;

                // Nếu backend trả về chuỗi đáp án đã lưu, parse lại để hiển thị
                var savedAnswersString = result.StudentSession?.StudentAnswersString;
                if (!string.IsNullOrWhiteSpace(savedAnswersString))
                {
                    RestoreAnswersFromString(savedAnswersString);
                }
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

    private bool _isSubmitting = false;

    private async Task SubmitExam()
    {
        if (_isSubmitting)
            return; // tránh nộp nhiều lần

        if (!StudentExamSessionId.HasValue)
        {
            Snackbar.Add("Không xác định được ca thi để nộp.", Severity.Error);
            return;
        }

        _isSubmitting = true;
        StateHasChanged();

        Snackbar.Add("Đang nộp bài...", Severity.Info);

        var request = new SubmitExamRequest
        {
            StudentExamSessionId = StudentExamSessionId.Value
        };

        var result = await StudentService.SubmitExamAsync(request);

        if (result == null)
        {
            Snackbar.Add("Không nhận được phản hồi từ server.", Severity.Error);
            _isSubmitting = false;
            return;
        }

        Snackbar.Add(result.Message, Severity.Success);

        Navigation.NavigateTo($"/Exam/Result?studentExamSessionId={StudentExamSessionId.Value}");

        _isSubmitting = false;
        StateHasChanged();
    }

    
    private string GetQuestionLabel(int questionId) =>
        _questionLabelMap.TryGetValue(questionId, out var label) ? label : string.Empty;

    /// <summary>
    /// Parse chuỗi studentAnswersString dạng "(1:1);(2:6);..." 
    /// thành dictionary _questionAnswers với key = OriginalExamPaperDetailId, value = answerId.
    /// </summary>
    /// <param name="answersString"></param>
    private void RestoreAnswersFromString(string answersString)
    {
        // Ví dụ chuỗi: "(1:1);(2:6);(3:10);..."
        var segments = answersString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var rawSegment in segments)
        {
            var segment = rawSegment.Trim();
            if (string.IsNullOrWhiteSpace(segment))
            {
                continue;
            }

            // Bỏ ngoặc tròn nếu có
            if (segment.StartsWith('(') && segment.EndsWith(')') && segment.Length >= 3)
            {
                segment = segment[1..^1];
            }

            var parts = segment.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                continue;
            }

            if (!int.TryParse(parts[0], out var questionId))
            {
                continue;
            }

            // Nếu value trống => chưa chọn
            if (string.IsNullOrWhiteSpace(parts[1]))
            {
                continue;
            }

            if (!int.TryParse(parts[1], out var answerId))
            {
                continue;
            }

            // Lưu vào map để GetSelectedAnswerId sử dụng
            _questionAnswers[questionId] = answerId;
        }
    }

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

        // Giá trị từ UI: với câu hỏi 1 đáp án sẽ là int (answerId)
        int? answerId = null;
        if (payload.value is int intValue)
        {
            answerId = intValue;
        }

        // Tự động chuyển câu ngay khi chọn đáp án (nếu bật autoNext)
        // Không cần đợi lưu thành công để trải nghiệm mượt mà hơn
        if (_autoNext && answerId.HasValue && _activeQuestionIndex < _questionDisplayItems.Count - 1)
        {
            // Đợi một chút để người dùng thấy đáp án đã được chọn
            await Task.Delay(300);
            NextQuestion();
            StateHasChanged();
        }

        // Nếu chưa có session id thì không gọi API
        if (!StudentExamSessionId.HasValue)
        {
            return;
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

    private void ToggleAutoNext()
    {
        _autoNext = !_autoNext;
        StateHasChanged();
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

    /// <summary>
    /// Xử lý vi phạm: đếm số lần và tự động nộp bài sau 3 lần
    /// </summary>
    private async Task HandleViolationAsync(string activityType, string warningMessage)
    {
        Console.WriteLine($"[HandleViolation] Activity: {activityType}, Current count: {_violationCount}");
        
        // Ghi nhận hoạt động
        await RecordActivityAsync(activityType, GetActivityDescription(activityType));
        
        // Chỉ đếm vi phạm nếu là hành động nghiêm trọng
        if (_violationTypes.Contains(activityType))
        {
            _violationCount++;
            Console.WriteLine($"[HandleViolation] Violation count increased to: {_violationCount}/{MAX_VIOLATIONS}");
            
            // Hiển thị cảnh báo với số lần vi phạm
            var severity = _violationCount >= MAX_VIOLATIONS ? Severity.Error : Severity.Warning;
            var message = $"{warningMessage}\n" +
                         $"Vi phạm: {_violationCount}/{MAX_VIOLATIONS} lần. " +
                         (_violationCount >= MAX_VIOLATIONS 
                             ? "Bài thi sẽ tự động nộp!" 
                             : $"Còn {MAX_VIOLATIONS - _violationCount} lần, bài thi sẽ tự động nộp!");
            
            Snackbar.Add(message, severity);
            await InvokeAsync(StateHasChanged);
            
            // Cập nhật UI để hiển thị bộ đếm vi phạm
            await InvokeAsync(StateHasChanged);
            
            // Tự động nộp bài sau 3 lần vi phạm
            if (_violationCount >= MAX_VIOLATIONS)
            {
                Console.WriteLine($"[HandleViolation] ⚠️ MAX VIOLATIONS REACHED! Auto-submitting exam...");
                
                Snackbar.Add("Đã vi phạm 3 lần. Hệ thống sẽ tự động nộp bài trong giây lát...", Severity.Error);
                await InvokeAsync(StateHasChanged);
                
                // Đợi một chút để người dùng thấy thông báo và bộ đếm
                await Task.Delay(3000);
                
                // Gọi submit trực tiếp
                try
                {
                    Console.WriteLine($"[HandleViolation] Calling SubmitExam()...");
                    Console.WriteLine($"[HandleViolation] StudentExamSessionId: {StudentExamSessionId}, _isSubmitting: {_isSubmitting}");
                    
                    // Gọi JS để force submit như backup (chạy song song)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(100);
                            await JS.InvokeVoidAsync("examFullscreen.forceSubmitExam");
                            Console.WriteLine("[HandleViolation] ✅ JS force submit called");
                        }
                        catch (Exception jsEx)
                        {
                            Console.WriteLine($"[HandleViolation] ❌ JS force submit failed: {jsEx.Message}");
                        }
                    });
                    
                    // Gọi submit từ C# (chính) - đảm bảo không bị chặn bởi _isSubmitting
                    if (_isSubmitting)
                    {
                        Console.WriteLine("[HandleViolation] ⚠️ Already submitting, skipping...");
                        return;
                    }
                    
                    await SubmitExam();
                    Console.WriteLine("[HandleViolation] ✅ SubmitExam() completed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HandleViolation] ❌ Error auto-submitting: {ex.Message}");
                    Console.WriteLine($"[HandleViolation] StackTrace: {ex.StackTrace}");
                    await InvokeAsync(() =>
                    {
                        Snackbar.Add($"Lỗi khi tự động nộp bài: {ex.Message}. Vui lòng nộp bài thủ công.", Severity.Error);
                    });
                }
            }
        }
        else
        {
            // Các hành động khác chỉ cảnh báo, không đếm vi phạm
            Snackbar.Add(warningMessage, Severity.Warning);
        }
    }

    /// <summary>
    /// Method được gọi từ JS để force submit exam
    /// </summary>
    [JSInvokable]
    public async Task ForceSubmitExam()
    {
        Console.WriteLine("[ForceSubmitExam] Called from JS");
        await SubmitExam();
    }

    private string GetActivityDescription(string activityType)
    {
        return activityType switch
        {
            "TabSwitch" => "Rời khỏi tab thi",
            "FullscreenExit" => "Thoát khỏi chế độ toàn màn hình",
            "Copy" => "Phát hiện sao chép nội dung",
            "Paste" => "Phát hiện dán nội dung",
            "RightClick" => "Phát hiện click chuột phải",
            "DevTools" => "Phát hiện mở DevTools",
            "Screenshot" => "Phát hiện chụp màn hình",
            _ => $"Phát hiện hành động: {activityType}"
        };
    }

    private async Task RecordActivityAsync(string activityType, string description, string? metadata = null)
    {
        if (!StudentExamSessionId.HasValue || StudentSession == null)
        {
            Console.WriteLine($"[RecordActivity] Missing StudentExamSessionId or StudentSession. SessionId: {StudentExamSessionId}, Session: {StudentSession != null}");
            return;
        }

        var studentCode = StudentSession.StudentCode ?? string.Empty;
        if (string.IsNullOrEmpty(studentCode))
        {
            Console.WriteLine($"[RecordActivity] StudentCode is empty. StudentSession: {System.Text.Json.JsonSerializer.Serialize(StudentSession)}");
            return;
        }

        try
        {
            var dto = new RecordActivityDto
            {
                StudentExamSessionId = StudentExamSessionId.Value,
                StudentCode = studentCode,
                ActivityType = activityType,
                Description = description,
                Metadata = metadata
            };

            Console.WriteLine($"[RecordActivity] Sending activity: Type={activityType}, StudentCode={studentCode}, SessionId={StudentExamSessionId.Value}");
            var result = await StudentActivityService.RecordActivityAsync(dto);
            Console.WriteLine($"[RecordActivity] Result: {result}");
        }
        catch (Exception ex)
        {
            // Log chi tiết để debug
            Console.WriteLine($"[RecordActivity] Error recording activity: {ex.Message}");
            Console.WriteLine($"[RecordActivity] StackTrace: {ex.StackTrace}");
        }
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