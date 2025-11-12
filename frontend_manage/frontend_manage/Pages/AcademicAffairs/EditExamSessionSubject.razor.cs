using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.DTOs;
using frontend_manage.Services.AcademicAffairs;
using frontend_manage.Services.ExamManager;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using MudBlazor;
using System.Net.Http;
using System.Net.Http.Json;

namespace frontend_manage.Pages.AcademicAffairs;

public partial class EditExamSessionSubject : ComponentBase
{
    [Parameter] public int Id { get; set; }
    
    [Inject] private ExamSessionSubjectService ExamSessionSubjectService { get; set; } = default!;
    [Inject] private ExamSessionService ExamSessionService { get; set; } = default!;
    [Inject] private ExamManagerService ExamManagerService { get; set; } = default!;
    [Inject] private AuthService AuthService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;
    
    private ExamSessionSubjectDto? _examSessionSubject;
    private ExamSessionSubjectUpdateDto _updateDto = new();
    private List<OriginalExamPaperListItemDto> _originalExamPapers = new();
    private List<ExamSessionDto> _examSessions = new();
    private List<DepartmentDto> _departments = new();
    private List<ExamSessionDepartmentDto> _examSessionDepartments = new();
    private List<SubjectDto> _subjects = new();
    
    private int? _selectedExamSessionId;
    private string? _selectedDepartmentId;
    
    private bool _loading = true;
    private bool _loadingOriginalExams = false;
    private bool _loadingExamSessions = false;
    private bool _loadingDepartments = false;
    private bool _loadingSubjects = false;
    private bool _isSaving = false;
    private string startTimeString = "";
    private string endTimeString = "";
    
    public class SubjectDto
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        _loading = true;
        try
        {
            // Load all required data in parallel
            var loadTasks = new List<Task>
            {
                LoadExamSessions(),
                LoadDepartments(),
                LoadExamSessionDepartments(),
                LoadSubjects()
            };
            
            await Task.WhenAll(loadTasks);
            
            // Load ExamSessionSubject
            _examSessionSubject = await ExamSessionSubjectService.GetByIdAsync(Id);
            
            if (_examSessionSubject != null)
            {
                _updateDto.ExamSessionDepartmentId = _examSessionSubject.ExamSessionDepartmentId;
                _updateDto.SubjectId = _examSessionSubject.SubjectId;
                _updateDto.Duration = _examSessionSubject.Duration;
                _updateDto.OriginalExamPaperId = _examSessionSubject.OriginalExamPaperId;
                _updateDto.IsCompleted = _examSessionSubject.IsCompleted;
                _updateDto.StartTime = _examSessionSubject.StartTime;
                _updateDto.EndTime = _examSessionSubject.EndTime;
                _updateDto.ExamSessionSubjectCore = _examSessionSubject.ExamSessionSubjectCore;
                
                // Find the ExamSessionDepartment to set selected values
                var examSessionDept = _examSessionDepartments.FirstOrDefault(esd => esd.ExamSessionDepartmentId == _examSessionSubject.ExamSessionDepartmentId);
                if (examSessionDept != null)
                {
                    _selectedExamSessionId = examSessionDept.ExamSessionId;
                    _selectedDepartmentId = examSessionDept.DepartmentId;
                }
                
                startTimeString = _examSessionSubject.StartTime.ToString("yyyy-MM-ddTHH:mm");
                if (_examSessionSubject.EndTime.HasValue)
                {
                    endTimeString = _examSessionSubject.EndTime.Value.ToString("yyyy-MM-ddTHH:mm");
                }
            }
            
            // Load Original Exam Papers
            await LoadOriginalExamPapers();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi khi tải dữ liệu: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }
    
    private async Task LoadExamSessions()
    {
        _loadingExamSessions = true;
        try
        {
            _examSessions = await ExamSessionService.GetAllAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi khi tải danh sách ca thi: {ex.Message}", Severity.Warning);
        }
        finally
        {
            _loadingExamSessions = false;
        }
    }
    
    private async Task LoadDepartments()
    {
        _loadingDepartments = true;
        try
        {
            var httpClient = new HttpClient { BaseAddress = new Uri(Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5163/") };
            var token = await AuthService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            _departments = await httpClient.GetFromJsonAsync<List<DepartmentDto>>("/api/Department") ?? new();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi khi tải danh sách khoa: {ex.Message}", Severity.Warning);
            _departments = new();
        }
        finally
        {
            _loadingDepartments = false;
        }
    }
    
    private async Task LoadExamSessionDepartments()
    {
        try
        {
            var httpClient = new HttpClient { BaseAddress = new Uri(Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5163/") };
            var token = await AuthService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            _examSessionDepartments = await httpClient.GetFromJsonAsync<List<ExamSessionDepartmentDto>>("/api/ExamSessionDepartment") ?? new();
        }
        catch (Exception ex)
        {
            // ExamSessionDepartment endpoint might not exist, try to get from ExamSessionSubjects
            Snackbar.Add($"Lỗi khi tải danh sách ca thi - khoa: {ex.Message}", Severity.Warning);
            _examSessionDepartments = new();
        }
    }
    
    private async Task LoadSubjects()
    {
        _loadingSubjects = true;
        try
        {
            // Load subjects from ExamSessionSubjects to get unique subjects
            var examSessionSubjects = await ExamSessionSubjectService.GetAllAsync();
            _subjects = examSessionSubjects
                .Where(ess => ess.SubjectId > 0 && !string.IsNullOrEmpty(ess.SubjectName))
                .GroupBy(ess => ess.SubjectId)
                .Select(g => new SubjectDto
                {
                    SubjectId = g.Key,
                    SubjectName = g.First().SubjectName,
                    SubjectCode = string.Empty
                })
                .OrderBy(s => s.SubjectName)
                .ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi khi tải danh sách môn học: {ex.Message}", Severity.Warning);
            _subjects = new();
        }
        finally
        {
            _loadingSubjects = false;
        }
    }

    private async Task LoadOriginalExamPapers()
    {
        _loadingOriginalExams = true;
        try
        {
            _originalExamPapers = await ExamManagerService.GetAllOriginalExamPapersAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi khi tải danh sách đề gốc: {ex.Message}", Severity.Warning);
        }
        finally
        {
            _loadingOriginalExams = false;
            StateHasChanged();
        }
    }

    private async Task HandleSubmit()
    {
        if (_examSessionSubject == null) return;
        
        // Validate selected ExamSession and Department
        if (!_selectedExamSessionId.HasValue || string.IsNullOrEmpty(_selectedDepartmentId))
        {
            Snackbar.Add("Vui lòng chọn ca thi và khoa", Severity.Error);
            return;
        }
        
        // Validate SubjectId
        if (_updateDto.SubjectId == 0)
        {
            Snackbar.Add("Vui lòng chọn môn học", Severity.Error);
            return;
        }
        
        // Find ExamSessionDepartmentId from selected ExamSession and Department
        var examSessionDept = _examSessionDepartments.FirstOrDefault(esd => 
            esd.ExamSessionId == _selectedExamSessionId.Value && 
            esd.DepartmentId == _selectedDepartmentId);
        
        if (examSessionDept == null)
        {
            Snackbar.Add("Không tìm thấy ca thi - khoa tương ứng. Vui lòng kiểm tra lại.", Severity.Error);
            return;
        }
        
        _updateDto.ExamSessionDepartmentId = examSessionDept.ExamSessionDepartmentId;
        
        _isSaving = true;
        try
        {
            // Parse datetime strings
            if (DateTime.TryParse(startTimeString, out var startTime))
            {
                _updateDto.StartTime = startTime;
            }
            else
            {
                Snackbar.Add("Thời gian bắt đầu không hợp lệ", Severity.Error);
                _isSaving = false;
                return;
            }
            
            if (!string.IsNullOrEmpty(endTimeString) && DateTime.TryParse(endTimeString, out var endTime))
            {
                _updateDto.EndTime = endTime;
            }
            else
            {
                _updateDto.EndTime = null;
            }
            
            await ExamSessionSubjectService.UpdateAsync(Id, _updateDto);
            Snackbar.Add("Cập nhật ca thi môn học thành công", Severity.Success);
            
            // Navigate back after a short delay
            await Task.Delay(500);
            NavigationManager.NavigateTo("/academic-affairs/dashboard");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo("/academic-affairs/dashboard");
    }
}

