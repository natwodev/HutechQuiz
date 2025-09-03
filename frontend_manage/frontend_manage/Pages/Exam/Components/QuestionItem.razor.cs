using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using frontend_manage.DTOs;
using System.Timers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Web;
using frontend_manage.Services;

namespace frontend_manage.Pages.Exam.Components
{
    public partial class QuestionItem : BaseComponent, IDisposable
    {
        [Parameter] public QuestionStructureDto? Question { get; set; }
        [Parameter] public int QuestionNumber { get; set; }
        [Parameter] public int StartChildQuestionNumber { get; set; }
        [Parameter] public Dictionary<int, string> SelectedAnswers { get; set; } = new();
        [Parameter] public Dictionary<int, string> SelectedChildAnswers { get; set; } = new();
        [Parameter] public EventCallback<AnswerSelectedArgs> OnAnswerSelected { get; set; }
        [Parameter] public EventCallback<AnswerSelectedArgs> OnChildAnswerSelected { get; set; }
        [Parameter] public EventCallback OnAnswersChanged { get; set; }
        [Parameter] public Func<string, string>? ProcessQuestionContentFunction { get; set; }
        
        // Audio
        [Parameter] public string? ShuffledExamPaperCore { get; set; }
        [Parameter] public string? BaseAddress { get; set; }

        // MathJax service
        [Inject] private IMathJaxService MathJaxService { get; set; } = default!;
        
        // JS Runtime
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        // Debounce dictionaries (theo từng câu hỏi)
        private readonly Dictionary<int, System.Timers.Timer> _debounceTimers = new();
        private readonly Dictionary<int, AnswerSelectedArgs> _pendingAnswers = new();
        private readonly Dictionary<int, bool> _isChildAnswer = new();
        private const int DEBOUNCE_DELAY_MS = 3000; // 3s

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

            var folderName = ShuffledExamPaperCore.Split('_')[0];
            var baseAddr = BaseAddress ?? "http://localhost:5163/";
            return $"{baseAddr}EPZ/{folderName}/{audioFileName}";
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await Task.Delay(500);
                try
                {
                    await JSRuntime.InvokeVoidAsync("MathJax.typesetPromise");
                    Console.WriteLine("MathJax typeset completed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error typesetting MathJax: {ex.Message}");
                }
            }
        }

        private string ProcessQuestionContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            var audioPattern = @"<audio>([^<]+)</audio>";
            var match = Regex.Match(content, audioPattern);
            
            if (match.Success)
            {
                var audioPath = match.Groups[1].Value;
                var fullAudioPath = GetAudioPath(audioPath);
                var audioId = $"audio_{audioPath.GetHashCode().ToString().Replace("-", "n")}";
                
                if (!string.IsNullOrEmpty(fullAudioPath))
                {
                    var audioButton = $@"
                    <div class=""audio-player mb-3"">
                        <audio id=""{audioId}"" style=""display: none;"">
                            <source src=""{fullAudioPath}"" type=""audio/mpeg"">
                        </audio>
                        <button class=""mud-button-root mud-button mud-button-filled mud-button-filled-primary mud-button-filled-size-medium mud-ripple"" 
                                onclick=""playAudioSimple('{audioId}', '{fullAudioPath}')"">
                            <span class=""mud-button-label"">🔊 Phát audio (5/5)</span>
                        </button>
                    </div>";
                    
                    content = Regex.Replace(content, audioPattern, audioButton);
                }
            }
            return content;
        }

        private int? GetSelectedAnswerAsInt(int originalExamPaperDetailId)
        {
            return SelectedAnswers.TryGetValue(originalExamPaperDetailId, out string? selectedValue) &&
                   int.TryParse(selectedValue, out int result)
                   ? result : null;
        }

        private int? GetSelectedChildAnswerAsInt(int originalExamPaperDetailId)
        {
            return SelectedChildAnswers.TryGetValue(originalExamPaperDetailId, out string? selectedValue) &&
                   int.TryParse(selectedValue, out int result)
                   ? result : null;
        }

        // ============= Debounced methods =============
        private async Task HandleAnswerSelectedWithDebounce(AnswerSelectedArgs args)
        {
            SelectedAnswers[args.QuestionId] = args.AnswerId.ToString();
            await InvokeAsync(() => StateHasChanged());
            await OnAnswersChanged.InvokeAsync();

            DebounceAnswer(args, isChild: false);
        }

        private async Task HandleChildAnswerSelectedWithDebounce(AnswerSelectedArgs args)
        {
            SelectedChildAnswers[args.QuestionId] = args.AnswerId.ToString();
            await InvokeAsync(() => StateHasChanged());
            await OnAnswersChanged.InvokeAsync();

            DebounceAnswer(args, isChild: true);
        }

        private void DebounceAnswer(AnswerSelectedArgs args, bool isChild)
        {
            _pendingAnswers[args.QuestionId] = args;
            _isChildAnswer[args.QuestionId] = isChild;

            if (_debounceTimers.TryGetValue(args.QuestionId, out var oldTimer))
            {
                oldTimer.Stop();
                oldTimer.Dispose();
            }

            var timer = new System.Timers.Timer(DEBOUNCE_DELAY_MS);
            timer.Elapsed += async (s, e) => await OnDebounceTimerElapsed(args.QuestionId);
            timer.AutoReset = false;
            timer.Start();

            _debounceTimers[args.QuestionId] = timer;
        }

        private async Task OnDebounceTimerElapsed(int questionId)
        {
            if (_pendingAnswers.TryGetValue(questionId, out var args))
            {
                await InvokeAsync(async () =>
                {
                    try
                    {
                        if (_isChildAnswer.TryGetValue(questionId, out var isChild) && isChild)
                        {
                            await OnChildAnswerSelected.InvokeAsync(args);
                        }
                        else
                        {
                            await OnAnswerSelected.InvokeAsync(args);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving answer for Q{questionId}: {ex.Message}");
                    }
                    finally
                    {
                        _pendingAnswers.Remove(questionId);
                        _isChildAnswer.Remove(questionId);
                        if (_debounceTimers.TryGetValue(questionId, out var timer))
                        {
                            timer.Dispose();
                            _debounceTimers.Remove(questionId);
                        }
                    }
                });
            }
        }

        public void Dispose()
        {
            foreach (var t in _debounceTimers.Values)
            {
                t.Stop();
                t.Dispose();
            }
            _debounceTimers.Clear();
        }
    }
}
