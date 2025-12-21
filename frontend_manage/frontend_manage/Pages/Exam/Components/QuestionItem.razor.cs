using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using frontend_manage.DTOs;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;

namespace frontend_manage.Pages.Exam.Components
{
    public partial class QuestionItem : ComponentBase
    {
        [Parameter] public QuestionStructureDto Question { get; set; } = new();
        [Parameter] public string? ShuffledExamPaperCore { get; set; }
        [Parameter] public EventCallback<(int questionId, object? value)> OnAnswered { get; set; }
        [Parameter] public string DisplayNumber { get; set; } = string.Empty;
        [Parameter] public int? SelectedAnswerId { get; set; }
        [Parameter] public Func<int, int?>? SelectedAnswerProvider { get; set; }
        [Parameter] public Func<int, string?>? LabelProvider { get; set; }

        [Inject] private IKaTeXService KaTeX { get; set; } = default!;
        [Inject] private IExamRenderingService ExamRenderingService { get; set; } = default!;

        // Dùng HttpClient để lấy BaseAddress backend (đã cấu hình qua ApiBaseUrl)
        [Inject] private HttpClient HttpClient { get; set; } = default!;
        [Inject] private IConfiguration Configuration { get; set; } = default!;

        private ElementReference _root;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Render KaTeX cho tất cả elements có class katex-content trong component này
            // KaTeX sẽ tự động xử lý [latex]...[/latex] tags thông qua katexInterop.js
            try
            {
                if (firstRender)
                {
                    // Sử dụng RenderWithRetryAsync để đảm bảo KaTeX đã sẵn sàng
                    await KaTeX.RenderWithRetryAsync(".katex-content", maxRetries: 3, delayMs: 100);
                }
                else
                {
                    // Re-render khi content thay đổi
            await KaTeX.RenderAsync(".katex-content");
                }
            }
            catch (Exception ex)
            {
                // Log error nhưng không throw để không làm gián đoạn UI
                System.Diagnostics.Debug.WriteLine($"Error rendering KaTeX: {ex.Message}");
            }
        }

        protected override void OnParametersSet()
        {
            var resolved = SelectedAnswerId ?? SelectedAnswerProvider?.Invoke(Question.OriginalExamPaperDetailId);
            if (resolved != _selectedSingle)
            {
                _selectedSingle = resolved;
            }
        }

        protected bool IsMatching(QuestionStructureDto q)
        {
            // Kiểm tra marker [matching] trong QuestionContent
            if ((q.QuestionContent ?? string.Empty).Contains("[matching]", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            // Fallback: Nếu có nhiều Answers (>= 4) và không phải MCQ pattern, có thể là matching
            // Matching questions thường có số lượng Answers chẵn và >= 4
            if (q.Answers != null && q.Answers.Count >= 4 && q.Answers.Count % 2 == 0)
            {
                // Kiểm tra xem có pattern matching không (ví dụ: có "A:" và "B:" trong content)
                var content = q.QuestionContent ?? string.Empty;
                if (content.Contains("A:", StringComparison.OrdinalIgnoreCase) && 
                    content.Contains("B:", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }

        protected string NormalizeLatex(string? content)
        {
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;

            // Sử dụng ExamRenderingService để normalize và render content
            return ExamRenderingService.NormalizeAndRenderContent(content, ShuffledExamPaperCore);
        }

        protected bool IsGroupParent(QuestionStructureDto q)
        {
            return q.ChildQuestions != null && q.ChildQuestions.Count > 0;
        }

        /// <summary>
        /// Kiểm tra xem đây có phải là matching question dạng parent-child không
        /// (parent có child questions và tất cả child đều có cùng số lượng answers)
        /// </summary>
        protected bool IsMatchingParent(QuestionStructureDto q)
        {
            if (q.ChildQuestions == null || q.ChildQuestions.Count == 0)
                return false;

            // Parent question không nên có answers (chỉ có stem)
            if (q.Answers != null && q.Answers.Count > 0)
                return false;

            // Kiểm tra xem tất cả child questions có cùng số lượng answers không
            var firstChild = q.ChildQuestions.First();
            if (firstChild.Answers == null || firstChild.Answers.Count == 0)
                return false;

            var answerCount = firstChild.Answers.Count;
            
            // Tất cả child questions phải có cùng số lượng answers
            if (q.ChildQuestions.Any(child => child.Answers == null || child.Answers.Count != answerCount))
                return false;

            // Kiểm tra thêm: parent question có stem chứa từ khóa về matching không
            // Hoặc tất cả child questions có cùng pattern (đều là MCQ với cùng số lượng answers)
            var stem = q.QuestionContent ?? string.Empty;
            var hasMatchingKeywords = stem.Contains("Nối cột", StringComparison.OrdinalIgnoreCase) ||
                                     stem.Contains("nối", StringComparison.OrdinalIgnoreCase) ||
                                     stem.Contains("match", StringComparison.OrdinalIgnoreCase);

            // Nếu có từ khóa matching hoặc pattern đặc biệt (số lượng child >= 2 và số lượng answers >= 2)
            if (hasMatchingKeywords || (q.ChildQuestions.Count >= 2 && answerCount >= 2))
            {
                // Đảm bảo tất cả child questions đều là MCQ (có answers)
                return q.ChildQuestions.All(child => 
                    child.Answers != null && 
                    child.Answers.Count > 0 &&
                    child.Answers.Count == answerCount);
            }

            return false;
        }

        /// <summary>
        /// Lấy left items cho matching question (từ child questions)
        /// </summary>
        protected List<AnswerStructureDto> GetMatchingLeftItems(QuestionStructureDto parent)
        {
            var leftItems = new List<AnswerStructureDto>();
            
            if (parent.ChildQuestions == null)
                return leftItems;

            foreach (var child in parent.ChildQuestions.OrderBy(c => c.Order))
            {
                // Tạo AnswerStructureDto từ child question stem
                leftItems.Add(new AnswerStructureDto
                {
                    AnswerId = child.OriginalExamPaperDetailId, // Dùng question ID làm AnswerId
                    AnswerContent = child.QuestionContent ?? string.Empty,
                    Order = child.Order,
                    OriginalExamPaperDetailId = child.OriginalExamPaperDetailId
                });
            }

            return leftItems;
        }

        /// <summary>
        /// Lấy right items cho matching question (từ answers của child đầu tiên)
        /// Trả về TẤT CẢ answers từ child đầu tiên (tất cả child đều có cùng answers)
        /// </summary>
        protected List<AnswerStructureDto> GetMatchingRightItems(QuestionStructureDto parent)
        {
            if (parent.ChildQuestions == null || parent.ChildQuestions.Count == 0)
                return new List<AnswerStructureDto>();

            var firstChild = parent.ChildQuestions.OrderBy(c => c.Order).First();
            if (firstChild.Answers == null)
                return new List<AnswerStructureDto>();

            // Trả về answers từ child đầu tiên (tất cả child đều có cùng answers)
            // Giống với OriginalExamPaperPreviewPage và ShuffledExamPaperPreviewPage
            return firstChild.Answers
                .OrderBy(a => a.Order)
                .Select(a => new AnswerStructureDto
                {
                    AnswerId = a.AnswerId,
                    AnswerContent = a.AnswerContent,
                    Order = a.Order,
                    OriginalExamPaperDetailId = a.OriginalExamPaperDetailId
                })
                .ToList();
        }

        /// <summary>
        /// Xử lý khi chọn matching pair cho matching parent-child question
        /// </summary>
        protected Task SelectMatchingChild(QuestionStructureDto parent, int leftAnswerId, int rightAnswerId)
        {
            // leftAnswerId thực chất là child question ID
            // rightAnswerId là answer ID từ cột B
            return OnAnswered.InvokeAsync((leftAnswerId, (object?)rightAnswerId));
        }

        protected int? _selectedSingle;

        protected Task SelectSingle(int questionId, int answerId)
        {
            _selectedSingle = answerId;
            return OnAnswered.InvokeAsync((questionId, (object?)answerId));
        }

        protected Task SelectMatching(int questionId, int leftAnswerId, string rightAnswerId)
        {
            // Với matching question, lưu rightAnswerId (string có thể parse thành int)
            // Để navigation biết đã trả lời, lưu object hoặc parsed int
            if (int.TryParse(rightAnswerId, out var rightId))
            {
                // Gọi OnAnswered để cập nhật navigation
                return OnAnswered.InvokeAsync((questionId, (object?)rightId));
            }
            // Nếu không parse được, vẫn lưu object để navigation biết đã trả lời
            return OnAnswered.InvokeAsync((questionId, new { left = leftAnswerId, right = rightAnswerId }));
        }

        protected async Task HandleMatchingPairsChanged(int questionId, List<(int leftId, int rightId)> pairs)
        {
            // Khi có thay đổi pairs, serialize tất cả pairs và lưu
            if (pairs == null || pairs.Count == 0)
            {
                // Nếu không còn pairs, gọi OnAnswered với null để xóa đáp án
                await OnAnswered.InvokeAsync((questionId, (object?)null));
                return;
            }

            // Serialize tất cả pairs thành JSON array
            try
            {
                var pairsArray = pairs.Select(p => new { left = p.leftId, right = p.rightId }).ToArray();
                var jsonString = System.Text.Json.JsonSerializer.Serialize(pairsArray);
                
                // Lưu JSON string vào _questionAnswers để navigation biết đã trả lời
                // Và gọi OnAnswered để trigger save
                // Với matching question, có thể cần lưu tất cả pairs, nhưng API có thể chỉ nhận int
                // Tạm thời lưu pair cuối cùng để tương thích với API hiện tại
                var lastPair = pairs.Last();
                await OnAnswered.InvokeAsync((questionId, (object?)lastPair.rightId));
            }
            catch
            {
                // Nếu serialize thất bại, vẫn lưu pair cuối cùng
                var lastPair = pairs.Last();
                await OnAnswered.InvokeAsync((questionId, (object?)lastPair.rightId));
            }
        }

        protected Task OnOptionKeyDown(KeyboardEventArgs e, int questionId, int answerId)
        {
            if (e.Key == "Enter" || e.Key == " ")
            {
                return SelectSingle(questionId, answerId);
            }
            return Task.CompletedTask;
        }
    }
}


