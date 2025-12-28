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
        [Parameter] public int? StudentExamSessionId { get; set; }
        [Parameter] public int? QuestionId { get; set; }

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
                    // Truyền _root để chỉ render trong phạm vi component này
                    await KaTeX.RenderElementAsync(new ElementReferenceWrapper(_root));
                }
                else
                {
                    // Re-render khi content thay đổi, chỉ trong phạm vi component này
                    await KaTeX.RenderElementAsync(new ElementReferenceWrapper(_root));
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
        /// Kiểm tra xem đây có phải là group question (câu hỏi nhóm) không
        /// Group question có pattern {<1>} — {<3>} trong nội dung
        /// </summary>
        protected bool IsGroupQuestion(QuestionStructureDto q)
        {
            if (q.ChildQuestions == null || q.ChildQuestions.Count == 0)
                return false;

            // Kiểm tra pattern {<...>} trong nội dung parent question
            // Pattern có thể là: {<1>} — {<3>} hoặc {<1>} - {<3>} hoặc {<1>}—{<3>}
            var stem = q.QuestionContent ?? string.Empty;
            
            // Kiểm tra nhiều pattern khác nhau cho group question
            var patterns = new[]
            {
                @"\{<\d+>\}.*?\{<\d+>\}",  // {<1>} ... {<3>}
                @"\{&lt;\d+&gt;\}.*?\{&lt;\d+&gt;\}",  // HTML encoded: {&lt;1&gt;} ... {&lt;3&gt;}
                @"\{&lt;\d+&gt;\}.*?—.*?\{&lt;\d+&gt;\}",  // HTML encoded với dấu gạch ngang
            };
            
            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(stem, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }

        protected int? _selectedSingle;

        protected Task SelectSingle(int questionId, int answerId)
        {
            _selectedSingle = answerId;
            return OnAnswered.InvokeAsync((questionId, (object?)answerId));
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

