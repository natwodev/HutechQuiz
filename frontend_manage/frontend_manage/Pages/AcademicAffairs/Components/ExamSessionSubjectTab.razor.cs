using frontend_manage.DTOs.AcademicAffairs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components;

public partial class ExamSessionSubjectTab : ComponentBase
{
    [Parameter]
    public List<ExamSessionSubjectDto> ExamSessionSubjects { get; set; } = new();
    
    [Parameter]
    public EventCallback OnExamSessionSubjectUpdated { get; set; }
    
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    
    private string searchString = "";

    private bool FilterFunc(ExamSessionSubjectDto subject)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;
        
        return subject.SubjectName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
               subject.ExamSessionSubjectCore.Contains(searchString, StringComparison.OrdinalIgnoreCase);
    }

    private async Task EditExamSessionSubject(ExamSessionSubjectDto subject)
    {
        var absoluteUrl = NavigationManager.ToAbsoluteUri($"/academic-affairs/exam-session-subject/edit/{subject.ExamSessionSubjectId}").ToString();
        await JSRuntime.InvokeVoidAsync("window.open", absoluteUrl, "_blank");
    }
}

