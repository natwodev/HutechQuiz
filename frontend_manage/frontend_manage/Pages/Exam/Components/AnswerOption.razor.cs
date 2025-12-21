using frontend_manage.DTOs;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;

namespace frontend_manage.Pages.Exam.Components
{
    public partial class AnswerOption : ComponentBase
    {
        [Parameter] public AnswerStructureDto Answer { get; set; } = new();
        [Parameter] public char Letter { get; set; }
        [Parameter] public int SelectedAnswerId { get; set; }
        [Parameter] public string GroupName { get; set; } = string.Empty;
        [Parameter] public EventCallback<int> OnSelect { get; set; }
        [Parameter] public string? ShuffledExamPaperCore { get; set; }
        [Parameter] public string? OriginalExamPaperCore { get; set; }

        [Inject] private IExamRenderingService ExamRenderingService { get; set; } = default!;

        private string NormalizeContent(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            return ExamRenderingService.NormalizeAndRenderContent(content, ShuffledExamPaperCore, OriginalExamPaperCore);
        }
    }
}



