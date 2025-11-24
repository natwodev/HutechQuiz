using System;
using frontend_manage.DTOs;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace frontend_manage.Pages.Exam.Components
{
    public partial class QuestionItem : ComponentBase
    {
        [Parameter] public QuestionStructureDto Question { get; set; } = new();
        [Parameter] public EventCallback<(int questionId, object? value)> OnAnswered { get; set; }
        [Parameter] public string DisplayNumber { get; set; } = string.Empty;
        [Parameter] public int? SelectedAnswerId { get; set; }
        [Parameter] public Func<int, int?>? SelectedAnswerProvider { get; set; }
        [Parameter] public Func<int, string?>? LabelProvider { get; set; }

        [Inject] private IKaTeXService KaTeX { get; set; } = default!;

        private ElementReference _root;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await KaTeX.RenderAsync(".katex-content");
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
            return (q.QuestionContent ?? string.Empty).Contains("[matching]", StringComparison.OrdinalIgnoreCase);
        }

        protected string NormalizeLatex(string? content)
        {
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;
            return content;
        }

        protected bool IsGroupParent(QuestionStructureDto q)
        {
            return q.ChildQuestions != null && q.ChildQuestions.Count > 0;
        }

        protected int? _selectedSingle;

        protected Task SelectSingle(int questionId, int answerId)
        {
            _selectedSingle = answerId;
            return OnAnswered.InvokeAsync((questionId, (object?)answerId));
        }

        protected Task SelectMatching(int questionId, int leftAnswerId, string rightAnswerId)
        {
            return OnAnswered.InvokeAsync((questionId, new { left = leftAnswerId, right = rightAnswerId }));
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


