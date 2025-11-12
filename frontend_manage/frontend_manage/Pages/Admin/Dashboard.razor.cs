using frontend_manage.DTOs;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services;
using frontend_manage.Services.AcademicAffairs;
using frontend_manage.Services.Admin;
using frontend_manage.Services.ExamManager;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Net.Http.Json;

namespace frontend_manage.Pages.Admin;

public partial class Dashboard : ComponentBase
{
    [Inject] private AcademicYearService AcademicYearService { get; set; } = default!;
    [Inject] private SemesterService SemesterService { get; set; } = default!;
    [Inject] private ExamBatchService ExamBatchService { get; set; } = default!;
    [Inject] private ExamSessionService ExamSessionService { get; set; } = default!;
    [Inject] private ExamSessionSubjectService ExamSessionSubjectService { get; set; } = default!;
    [Inject] private LecturerService LecturerService { get; set; } = default!;
    [Inject] private AdminStudentService AdminStudentService { get; set; } = default!;
    [Inject] private SystemService SystemService { get; set; } = default!;
    [Inject] private ExamManagerService ExamManagerService { get; set; } = default!;
    [Inject] private AuthService AuthService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private bool _loading = true;
    
    // Academic Affairs Data
    private List<AcademicYearDto> _academicYears = new();
    private List<SemesterDto> _semesters = new();
    private List<DepartmentDto> _departments = new();
    private List<ExamBatchDto> _examBatches = new();
    private List<ExamSessionDto> _examSessions = new();
    private List<ExamSessionSubjectDto> _examSessionSubjects = new();
    
    // Exam Management Data - Components handle their own data loading
    
    // Student & Lecturer Data
    private List<StudentInfoDto> _students = new();
    private List<LecturerDto> _lecturers = new();
    
    // System Data
    private QueueStatusDto? _queueStatus;

    protected override async Task OnInitializedAsync()
    {
        var isAuthenticated = await AuthService.IsAuthenticated();
        if (!isAuthenticated)
        {
            NavigationManager.NavigateTo("/login");
            return;
        }

        var isAdmin = await AuthService.IsAdmin();
        if (!isAdmin)
        {
            Snackbar.Add("Bạn không có quyền truy cập trang này", Severity.Error);
            NavigationManager.NavigateTo("/login");
            return;
        }

        await RefreshData();
    }

    private async Task RefreshData()
    {
        _loading = true;
        StateHasChanged();

        try
        {
            // Load Academic Affairs data
            _academicYears = await AcademicYearService.GetAllAsync();
            _semesters = await SemesterService.GetAllAsync();
            _examBatches = await ExamBatchService.GetAllAsync();
            _examSessions = await ExamSessionService.GetAllAsync();
            _examSessionSubjects = await ExamSessionSubjectService.GetAllAsync();
            
            // Load Departments
            await LoadDepartments();
            
            // Exam Papers are loaded by their own components
            
            // Load Students
            _students = await AdminStudentService.GetAllStudentsAsync();
            
            // Load Lecturers
            _lecturers = await LecturerService.GetAllLecturersAsync();
            
            // Load System Status
            _queueStatus = await SystemService.CheckQueueStatusAsync();
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

    private async Task LoadDepartments()
    {
        try
        {
            var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5163/") };
            var token = await AuthService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            _departments = await httpClient.GetFromJsonAsync<List<DepartmentDto>>("/api/Department") ?? new();
        }
        catch
        {
            _departments = new();
        }
    }

    private async Task Logout()
    {
        await AuthService.Logout();
        NavigationManager.NavigateTo("/login");
    }
}

