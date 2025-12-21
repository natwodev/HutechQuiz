using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services;
using frontend_manage.Services.Admin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class ExamSessionSubjectTab : ComponentBase
{
    [Parameter]
    public List<ExamSessionSubjectDto> ExamSessionSubjects { get; set; } = new();
    
    [Parameter]
    public EventCallback OnExamSessionSubjectUpdated { get; set; }
    
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private AuthService AuthService { get; set; } = default!;
    [Inject] private AdminExamSessionSubjectService ExamSessionSubjectService { get; set; } = default!;
    
    private string searchString = "";

    private bool FilterFunc(ExamSessionSubjectDto subject)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;
        
        return subject.SubjectName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
               subject.ExamSessionSubjectCore.Contains(searchString, StringComparison.OrdinalIgnoreCase);
    }

    private async Task CreateExamSessionSubject()
    {
        var parameters = new DialogParameters
        {
            ["OnCreated"] = EventCallback.Factory.Create<ExamSessionSubjectDto>(this, async (created) =>
            {
                await OnExamSessionSubjectUpdated.InvokeAsync();
                Snackbar.Add("Tạo ca thi môn học thành công!", Severity.Success);
            })
        };

        var dialog = await DialogService.ShowAsync<CreateExamSessionSubjectDialog>("Tạo mới Ca thi môn học", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled)
        {
            await OnExamSessionSubjectUpdated.InvokeAsync();
        }
    }

    private async Task EditExamSessionSubject(ExamSessionSubjectDto subject)
    {
        // Kiểm tra role để điều hướng đến route đúng
        var roles = await AuthService.GetUserRolesFromToken();
        string route;
        
        if (roles.Contains("Admin"))
        {
            route = $"/admin/exam-session-subject/edit/{subject.ExamSessionSubjectId}";
        }
        else if (roles.Contains("AcademicAffairs"))
        {
            route = $"/academic-affairs/exam-session-subject/edit/{subject.ExamSessionSubjectId}";
        }
        else
        {
            // Fallback
            route = $"/academic-affairs/exam-session-subject/edit/{subject.ExamSessionSubjectId}";
        }
        
        var absoluteUrl = NavigationManager.ToAbsoluteUri(route).ToString();
        await JSRuntime.InvokeVoidAsync("window.open", absoluteUrl, "_blank");
    }
}