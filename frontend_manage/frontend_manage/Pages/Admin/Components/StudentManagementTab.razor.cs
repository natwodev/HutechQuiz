using frontend_manage.DTOs;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.Admin;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class StudentManagementTab : ComponentBase
{
    [Parameter]
    public List<StudentInfoDto> Students { get; set; } = new();
    
    [Parameter]
    public List<ExamSessionSubjectDto> ExamSessionSubjects { get; set; } = new();
    
    [Parameter]
    public EventCallback OnStudentUpdated { get; set; }
    
    [Inject] private AdminStudentService AdminStudentService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    
    private string searchString = "";
    private int? _selectedExamSessionSubjectId
    {
        get => _selectedExamSessionSubjectIdValue;
        set
        {
            if (_selectedExamSessionSubjectIdValue != value)
            {
                _selectedExamSessionSubjectIdValue = value;
                _ = LoadStudentsByExamSessionSubject();
            }
        }
    }
    private int? _selectedExamSessionSubjectIdValue;
    private List<StudentInfoDto> _filteredStudentsByExamSession = new();
    private bool _isLoadingFilteredStudents = false;

    protected override void OnParametersSet()
    {
        UpdateFilteredStudents();
    }

    private List<StudentInfoDto> FilteredStudents
    {
        get
        {
            if (_selectedExamSessionSubjectIdValue.HasValue)
            {
                return _filteredStudentsByExamSession;
            }
            return Students;
        }
    }

    private async Task LoadStudentsByExamSessionSubject()
    {
        if (!_selectedExamSessionSubjectIdValue.HasValue)
        {
            _filteredStudentsByExamSession = new List<StudentInfoDto>();
            StateHasChanged();
            return;
        }

        _isLoadingFilteredStudents = true;
        StateHasChanged();
        try
        {
            var (subject, students) = await AdminStudentService.GetStudentsByExamSessionSubjectAsync(_selectedExamSessionSubjectIdValue.Value);
            
            // Convert StudentExamRoomStatusDto to StudentInfoDto
            _filteredStudentsByExamSession = students.Select(s => new StudentInfoDto
            {
                StudentCode = s.StudentCode,
                FirstName = s.FirstName,
                LastName = s.LastName,
                IsLogin = s.IsLogin,
                DateOfBirth = null, // Không có trong response
                Gender = null // Không có trong response
            }).ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi khi tải danh sách sinh viên: {ex.Message}", Severity.Error);
            _filteredStudentsByExamSession = new List<StudentInfoDto>();
        }
        finally
        {
            _isLoadingFilteredStudents = false;
            StateHasChanged();
        }
    }

    private void UpdateFilteredStudents()
    {
        StateHasChanged();
    }

    private bool FilterFunc(StudentInfoDto student)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;
        
        return student.StudentCode.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
               student.FirstName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
               student.LastName.Contains(searchString, StringComparison.OrdinalIgnoreCase);
    }

    private async Task ToggleLoginStatus(StudentInfoDto student)
    {
        try
        {
            var newStatus = !(student.IsLogin == true);
            var success = await AdminStudentService.ActiveLoginAsync(student.StudentCode, newStatus);
            if (success)
            {
                Snackbar.Add($"Đã {(newStatus ? "kích hoạt" : "vô hiệu hóa")} đăng nhập cho sinh viên {student.StudentCode}", Severity.Success);
                await OnStudentUpdated.InvokeAsync();
            }
            else
            {
                Snackbar.Add("Lỗi khi cập nhật trạng thái đăng nhập", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }

    private async Task ViewStudentDetails(StudentInfoDto student)
    {
        var parameters = new DialogParameters { ["Student"] = student };
        var dialog = await DialogService.ShowAsync<StudentDetailsDialog>("Chi tiết sinh viên", parameters);
    }

    private async Task OpenImportDialog()
    {
        var parameters = new DialogParameters();
        
        // Nếu đang filter theo ca thi môn học, tìm Core tương ứng để truyền vào dialog
        if (_selectedExamSessionSubjectIdValue.HasValue)
        {
            var selectedSubject = ExamSessionSubjects.FirstOrDefault(e => e.ExamSessionSubjectId == _selectedExamSessionSubjectIdValue.Value);
            if (selectedSubject != null)
            {
                parameters.Add("DefaultExamSessionSubjectCore", selectedSubject.ExamSessionSubjectCore);
            }
        }
        
        var dialog = await DialogService.ShowAsync<StudentImportDialog>("Import sinh viên từ Excel", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnStudentUpdated.InvokeAsync();
        }
    }
}


