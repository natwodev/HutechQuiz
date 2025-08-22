using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs;
using System.Timers;

namespace frontend_manage.Pages.Exam.Components
{
    public partial class QuestionItem : ComponentBase, IDisposable
    {
        [Parameter] public QuestionStructureDto? Question { get; set; }
        [Parameter] public int QuestionNumber { get; set; }
        [Parameter] public int StartChildQuestionNumber { get; set; }
        [Parameter] public Dictionary<int, string> SelectedAnswers { get; set; } = new();
        [Parameter] public Dictionary<int, string> SelectedChildAnswers { get; set; } = new();
        [Parameter] public EventCallback<AnswerSelectedArgs> OnAnswerSelected { get; set; }
        [Parameter] public EventCallback<AnswerSelectedArgs> OnChildAnswerSelected { get; set; }
        [Parameter] public Func<string, string>? ProcessQuestionContentFunction { get; set; }

        // Debounce timer properties
        private System.Timers.Timer? _debounceTimer;
        private AnswerSelectedArgs? _pendingAnswerArgs;
        private bool _isPendingChildAnswer;
        private const int DEBOUNCE_DELAY_MS = 3000; // 3 seconds

        public class AnswerSelectedArgs
        {
            public int QuestionId { get; set; }
            public int AnswerId { get; set; }
        }

        private int GetChildQuestionNumber(QuestionStructureDto childQuestion)
        {
            if (Question?.ChildQuestions == null) return StartChildQuestionNumber;
            
            var childIndex = Question.ChildQuestions.OrderBy(cq => cq.Order).ToList().IndexOf(childQuestion);
            return StartChildQuestionNumber + childIndex;
        }

        private string ProcessQuestionContent(string content)
        {
            if (ProcessQuestionContentFunction != null)
            {
                return ProcessQuestionContentFunction(content);
            }
            return content ?? string.Empty;
        }

        private int? GetSelectedAnswerAsInt(int originalExamPaperDetailId)
        {
            SelectedAnswers.TryGetValue(originalExamPaperDetailId, out string? selectedValue);
            if (string.IsNullOrEmpty(selectedValue))
                return null;
            
            if (int.TryParse(selectedValue, out int result))
                return result;
            
            return null;
        }

        private int? GetSelectedChildAnswerAsInt(int originalExamPaperDetailId)
        {
            SelectedChildAnswers.TryGetValue(originalExamPaperDetailId, out string? selectedValue);
            if (string.IsNullOrEmpty(selectedValue))
                return null;
            
            if (int.TryParse(selectedValue, out int result))
                return result;
            
            return null;
        }

        // Debounced methods for handling answer selection
        private async Task HandleAnswerSelectedWithDebounce(AnswerSelectedArgs args)
        {
            // Cập nhật UI ngay lập tức (tích checkbox)
            SelectedAnswers[args.QuestionId] = args.AnswerId.ToString();
            StateHasChanged();
            
            // Lưu args để gọi API sau
            _pendingAnswerArgs = args;
            _isPendingChildAnswer = false;
            
            // Reset the timer
            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();
            
            _debounceTimer = new System.Timers.Timer(DEBOUNCE_DELAY_MS);
            _debounceTimer.Elapsed += async (sender, e) => await OnDebounceTimerElapsed();
            _debounceTimer.AutoReset = false;
            _debounceTimer.Start();
        }

        private async Task HandleChildAnswerSelectedWithDebounce(AnswerSelectedArgs args)
        {
            // Cập nhật UI ngay lập tức (tích checkbox)
            SelectedChildAnswers[args.QuestionId] = args.AnswerId.ToString();
            StateHasChanged();
            
            // Lưu args để gọi API sau
            _pendingAnswerArgs = args;
            _isPendingChildAnswer = true;
            
            // Reset the timer
            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();
            
            _debounceTimer = new System.Timers.Timer(DEBOUNCE_DELAY_MS);
            _debounceTimer.Elapsed += async (sender, e) => await OnDebounceTimerElapsed();
            _debounceTimer.AutoReset = false;
            _debounceTimer.Start();
        }

        private async Task OnDebounceTimerElapsed()
        {
            if (_pendingAnswerArgs != null)
            {
                await InvokeAsync(async () =>
                {
                    try
                    {
                        // Chỉ gọi API, không cập nhật UI nữa vì đã update trước đó
                        if (_isPendingChildAnswer)
                        {
                            await OnChildAnswerSelected.InvokeAsync(_pendingAnswerArgs);
                        }
                        else
                        {
                            await OnAnswerSelected.InvokeAsync(_pendingAnswerArgs);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error if needed
                        Console.WriteLine($"Error invoking answer selected callback: {ex.Message}");
                        
                        // Nếu API thất bại, có thể rollback UI state ở đây nếu cần
                        // Hoặc hiển thị thông báo lỗi cho user
                    }
                    finally
                    {
                        _pendingAnswerArgs = null;
                        // Không cần StateHasChanged() ở đây vì UI đã được update trước đó
                    }
                });
            }

            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }

        public void Dispose()
        {
            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();
        }
    }
}