using frontend_manage.DTOs;
using Microsoft.AspNetCore.Components;

namespace frontend_manage.Pages.Exam.Components;

public partial class QuestionDisplayPanel : ComponentBase
{
    [Parameter] public QuestionStructureDto? ActiveQuestion { get; set; }
    [Parameter] public string? ShuffledExamPaperCore { get; set; }
    [Parameter] public int CurrentIndex { get; set; }
    [Parameter] public int TotalQuestions { get; set; }
    [Parameter] public EventCallback OnPrevious { get; set; }
    [Parameter] public EventCallback OnNext { get; set; }
    [Parameter] public Func<int, string> GetQuestionLabel { get; set; } = _ => string.Empty;
    [Parameter] public Func<int, int?> GetSelectedAnswerId { get; set; } = _ => null;
    [Parameter] public EventCallback<(int questionId, object? value)> OnAnswered { get; set; }
    [Parameter] public StudentExamSessionCacheDto? StudentSession { get; set; }
    [Parameter] public EventCallback OnExamTimerExpired { get; set; }
}