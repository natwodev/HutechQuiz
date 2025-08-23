using Microsoft.AspNetCore.Components;

namespace frontend_manage.Pages.Exam.Components
{
    public partial class ExamTimer : ComponentBase
    {
        [Parameter] public string FormattedTime { get; set; } = "00:00";
        [Parameter] public int RemainingMinutes { get; set; }
        [Parameter] public int ExtraMinutes { get; set; }
        [Parameter] public bool IsTimeUp { get; set; }
        [Parameter] public bool IsSubmitDisabled { get; set; }
        [Parameter] public EventCallback OnSubmitExam { get; set; }
    }
}
