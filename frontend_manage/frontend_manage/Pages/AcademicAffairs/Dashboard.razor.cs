using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.Admin;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System;
using System.Linq;

namespace frontend_manage.Pages.AcademicAffairs;

public partial class Dashboard : ComponentBase
{
    [Inject] private AdminAcademicYearService AcademicYearService { get; set; } = default!;
    [Inject] private AdminSemesterService SemesterService { get; set; } = default!;
    [Inject] private AdminExamBatchService ExamBatchService { get; set; } = default!;
    [Inject] private AdminExamBatchDetailService ExamBatchDetailService { get; set; } = default!;
    [Inject] private AdminExamSessionService ExamSessionService { get; set; } = default!;
    [Inject] private AdminExamSessionSubjectService ExamSessionSubjectService { get; set; } = default!;
    [Inject] private AuthService AuthService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private bool _loading = true;
    
    // Academic Affairs Data
    private List<AcademicYearDto> _academicYears = new();
    private List<SemesterDto> _semesters = new();
    private List<ExamBatchDto> _examBatches = new();
    private List<ExamBatchDetailDto> _examBatchDetails = new();
    private List<ExamSessionDto> _examSessions = new();
    private List<ExamSessionSubjectDto> _examSessionSubjects = new();

    protected override async Task OnInitializedAsync()
    {
        var isAuthenticated = await AuthService.IsAuthenticated();
        if (!isAuthenticated)
        {
            NavigationManager.NavigateTo("/login");
            return;
        }

        var roles = await AuthService.GetUserRolesFromToken();
        var hasAccess = roles.Any(r =>
            string.Equals(r, "AcademicAffairs", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase));

        if (!hasAccess)
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
            _examBatchDetails = await ExamBatchDetailService.GetAllAsync();
            _examSessions = await ExamSessionService.GetAllAsync();
            _examSessionSubjects = await ExamSessionSubjectService.GetAllAsync();
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

    private async Task Logout()
    {
        await AuthService.Logout();
        NavigationManager.NavigateTo("/login");
    }
}
