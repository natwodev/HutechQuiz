using frontend_manage.DTOs;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading;

namespace frontend_manage.Pages.Exam.Components
{
    public partial class MatchingQuestion : ComponentBase, IAsyncDisposable
    {
        [Parameter] public List<AnswerStructureDto> LeftItems { get; set; } = new();
        [Parameter] public List<AnswerStructureDto> RightItems { get; set; } = new();
        [Parameter] public EventCallback<(int leftId, int rightId)> OnPair { get; set; }
        [Parameter] public EventCallback<List<(int leftId, int rightId)>> OnPairsChanged { get; set; }
        [Parameter] public string? ShuffledExamPaperCore { get; set; }
        [Parameter] public string? OriginalExamPaperCore { get; set; }
        [Parameter] public bool IsPreviewMode { get; set; } = false; // Preview mode: tự động vẽ đường nối theo đáp án đúng
        [Parameter] public Dictionary<int, int>? CorrectPairs { get; set; } // Dictionary<leftQuestionId, rightAnswerId> cho preview mode

        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IKaTeXService KaTeX { get; set; } = default!;
        [Inject] private IExamRenderingService ExamRenderingService { get; set; } = default!;

        private int? _pendingLeft;
        private string ContainerId = $"match-" + Guid.NewGuid().ToString("N");
        private List<(int leftId, int rightId)> _pairs = new();
        private bool _linesInitialized = false;

        private string NormalizeContent(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            // Loại bỏ số và chữ cái đầu nếu có (ví dụ: "1. Iodine" → "Iodine", "a. Starch indicator" → "Starch indicator")
            // Vì frontend sẽ tự động thêm số và chữ cái khi render
            var cleaned = content.Trim();
            
            // Loại bỏ số đầu: "1. ", "2. ", "10. ", etc.
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^\d+\.\s*", string.Empty);
            
            // Loại bỏ chữ cái đầu: "a. ", "b. ", "A. ", etc.
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^[a-z]\.\s*", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return ExamRenderingService.NormalizeAndRenderContent(cleaned, ShuffledExamPaperCore, OriginalExamPaperCore);
        }

        private async Task DrawLineAsync(int leftId, int rightId)
        {
            // Use LeaderLine to draw dynamic connection
            await JS.InvokeVoidAsync("matchingHelpers.addLeaderLine", ContainerId, $"left-{leftId}", $"right-{rightId}");
        }


        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Clear lines cũ khi first render để tránh lines từ lần render trước
            if (firstRender)
            {
                try
                {
                    await JS.InvokeVoidAsync("matchingHelpers.clearLeaderLines", ContainerId);
                    _pairs.Clear();
                }
                catch
                {
                    // Ignore errors
                }
            }

            // Render KaTeX cho matching content
            try
            {
                if (firstRender)
                {
                    await KaTeX.RenderWithRetryAsync(".katex-content", maxRetries: 3, delayMs: 100);
                }
                else
                {
                    await KaTeX.RenderAsync(".katex-content");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rendering KaTeX in MatchingQuestion: {ex.Message}");
            }

            // Nếu là preview mode và có đáp án đúng, tự động vẽ đường nối
            if (IsPreviewMode && CorrectPairs != null && CorrectPairs.Count > 0)
            {
                if (!_linesInitialized)
                {
                    // Clear lines cũ trước khi vẽ preview
                    await JS.InvokeVoidAsync("matchingHelpers.clearLeaderLines", ContainerId);
                    _pairs.Clear();
                    _linesInitialized = true;
                    _ = DrawPreviewLinesAsync();
                }
            }
            else if (!firstRender && _pairs.Count > 0 && !_linesInitialized)
            {
                // Chỉ re-draw nếu đã có pairs và chưa được initialize
                await Task.Delay(100);
                await JS.InvokeVoidAsync("matchingHelpers.redrawLeaderLines", ContainerId);
                _linesInitialized = true;
            }
        }

        private async Task SelectLeft(int leftId)
        {
            _pendingLeft = leftId;
            await InvokeAsync(StateHasChanged);
        }

        private async Task DrawPreviewLinesAsync()
        {
            try
            {
                // Đợi để DOM và KaTeX đã render xong
                await Task.Delay(500);
                
                // Clear các pairs cũ trước
                _pairs.Clear();
                
                // Vẽ từng cặp
                foreach (var pair in CorrectPairs!)
                {
                    var leftId = pair.Key;
                    var rightId = pair.Value;
                    
                    await DrawLineAsync(leftId, rightId);
                    _pairs.Add((leftId, rightId));
                }
                
                // Re-draw tất cả các đường nối sau khi đã vẽ xong
                await Task.Delay(200);
                await JS.InvokeVoidAsync("matchingHelpers.redrawLeaderLines", ContainerId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in preview line drawing: {ex.Message}");
            }
        }

        private async Task SelectRight(int rightId)
        {
            if (_pendingLeft.HasValue)
            {
                // Kiểm tra xem item bên phải đã được nối chưa
                var existingRightPair = _pairs.FirstOrDefault(p => p.rightId == rightId);
                if (existingRightPair.leftId != 0)
                {
                    // Item bên phải đã được nối, bỏ qua
                    _pendingLeft = null;
                    await InvokeAsync(StateHasChanged);
                    return;
                }
                
                // Kiểm tra xem item bên trái đã được nối chưa, nếu có thì xóa đường nối cũ
                var existingLeftPair = _pairs.FirstOrDefault(p => p.leftId == _pendingLeft.Value);
                if (existingLeftPair.leftId != 0)
                {
                    // Xóa đường nối cũ
                    try
                    {
                        await JS.InvokeVoidAsync("matchingHelpers.removeLeaderLineByLeft", ContainerId, _pendingLeft.Value);
                        _pairs.Remove(existingLeftPair);
                    }
                    catch
                    {
                        // Ignore errors
                    }
                }
                
                await OnPair.InvokeAsync((_pendingLeft.Value, rightId));
                await DrawLineAsync(_pendingLeft.Value, rightId);
                _pairs.Add((_pendingLeft.Value, rightId));
                await OnPairsChanged.InvokeAsync(_pairs);
                _pendingLeft = null;
                await InvokeAsync(StateHasChanged);
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                // Cleanup LeaderLines khi component bị dispose
                if (JS != null && !string.IsNullOrEmpty(ContainerId))
                {
                    await JS.InvokeVoidAsync("matchingHelpers.clearLeaderLines", ContainerId);
                }
            }
            catch
            {
                // Ignore errors
            }
        }
    }
}

