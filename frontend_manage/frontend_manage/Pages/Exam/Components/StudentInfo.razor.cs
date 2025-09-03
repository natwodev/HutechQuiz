using Microsoft.AspNetCore.Components;

namespace frontend_manage.Pages.Exam.Components
{
    public partial class StudentInfo : BaseComponent
    {
        [Parameter] public string StudentCode { get; set; } = string.Empty;
        [Parameter] public string ExamPaperCode { get; set; } = string.Empty;
        [Parameter] public string SubjectName { get; set; } = string.Empty;
        [Parameter] public string SubjectCode { get; set; } = string.Empty;
        [Parameter] public string RoomName { get; set; } = string.Empty;
        [Parameter] public int Duration { get; set; }
        [Parameter] public int ExtraMinutes { get; set; }
    }
}