using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using frontend_manage.DTOs;
using System.Timers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Web;

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
        
        // Audio processing parameters
        [Parameter] public string? ShuffledExamPaperCore { get; set; }
        [Parameter] public string? BaseAddress { get; set; }

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

        private string GetAudioPath(string audioFileName)
        {
            if (string.IsNullOrEmpty(ShuffledExamPaperCore) || string.IsNullOrEmpty(audioFileName))
                return string.Empty;

            // Lấy phần trước dấu _ từ ShuffledExamPaperCore
            var folderName = ShuffledExamPaperCore.Split('_')[0];
            
            // Tạo đường dẫn audio trực tiếp tới file trong backend
            var baseAddr = BaseAddress ?? "http://localhost:5163/";
            return $"{baseAddr}EPZ/{folderName}/{audioFileName}";
        }

        private string ProcessQuestionContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Tìm và thay thế thẻ audio
            var audioPattern = @"<audio>([^<]+)</audio>";
            var match = Regex.Match(content, audioPattern);
            
            if (match.Success)
            {
                var audioPath = match.Groups[1].Value;
                var fullAudioPath = GetAudioPath(audioPath);
                // Tạo audioId dựa trên audioPath để đảm bảo tính nhất quán
                var audioId = $"audio_{audioPath.GetHashCode().ToString().Replace("-", "n")}";
                
                if (!string.IsNullOrEmpty(fullAudioPath))
                {
                    // Thay thế thẻ audio bằng button đơn giản
                    var audioButton = $@"
                    <div class=""audio-player mb-3"">
                        <audio id=""{audioId}"" style=""display: none;"">
                            <source src=""{fullAudioPath}"" type=""audio/mpeg"">
                        </audio>
                        <button class=""mud-button-root mud-button mud-button-filled mud-button-filled-primary mud-button-filled-size-medium mud-ripple"" 
                                onclick=""playAudioSimple('{audioId}', '{fullAudioPath}')"">
                            <span class=""mud-button-label"">
                                🔊 Phát audio (5/5)
                            </span>
                        </button>
                    </div>";
                    
                    return Regex.Replace(content, audioPattern, audioButton);
                }
            }
            
            return content;
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