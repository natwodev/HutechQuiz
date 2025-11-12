using frontend_manage.DTOs.AcademicAffairs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class ExamSessionSubjectTab : ComponentBase
{
    [Parameter]
    public List<ExamSessionSubjectDto> ExamSessionSubjects { get; set; } = new();
    
    [Parameter]
    public EventCallback OnExamSessionSubjectUpdated { get; set; }
    
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    
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
        Snackbar.Add("Chức năng đang được phát triển", Severity.Info);
    }
}


