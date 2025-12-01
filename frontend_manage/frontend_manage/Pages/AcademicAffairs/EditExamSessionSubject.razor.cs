using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.DTOs;
using frontend_manage.Services.Admin;
using frontend_manage.Services.ExamManager;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;

namespace frontend_manage.Pages.AcademicAffairs;

public partial class EditExamSessionSubject : ComponentBase
{
    [Parameter] public int Id { get; set; }
    private string _currentRoleTag = "Academic Affairs";
    private IReadOnlyCollection<string> _userRoles = Array.Empty<string>();
    
    [Inject] private AdminExamSessionSubjectService ExamSessionSubjectService { get; set; } = default!;
    [Inject] private AdminExamSessionService ExamSessionService { get; set; } = default!;
    [Inject] private ExamRoomService ExamRoomService { get; set; } = default!;
    [Inject] private LecturerService LecturerService { get; set; } = default!;
    [Inject] private ExamManagerService ExamManagerService { get; set; } = default!;
    [Inject] private AuthService AuthService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;
    
    private ExamSessionSubjectDto? _examSessionSubject;
    private ExamSessionSubjectUpdateDto _updateDto = new();
    private List<OriginalExamPaperListItemDto> _originalExamPapers = new();
    private List<ExamSessionDto> _examSessions = new();
    private List<LecturerDto> _lecturers = new();
    private List<SubjectDto> _subjects = new();
    private List<ExamRoomDto> _examRooms = new();
    
    private int? _selectedExamSessionId;
    private int? _selectedMonitorId;
    private int? _selectedExamRoomId;
    
    private bool _loading = true;
    private bool _loadingOriginalExams = false;
    private bool _loadingExamSessions = false;
    private bool _loadingLecturers = false;
    private bool _loadingSubjects = false;
    private bool _loadingExamRooms = false;
    private bool _isSaving = false;
    private bool _isUpdatingMonitor = false;
    private string startTimeString = "";
    private string endTimeString = "";
    
    public class SubjectDto
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
    }

    private bool CanManageMonitor =>
        _userRoles.Any(r =>
            string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "AcademicAffairs", StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        await ResolveRoleTagAsync();
        await LoadData();
    }

    private async Task ResolveRoleTagAsync()
    {
        try
        {
            var roles = await AuthService.GetUserRolesFromToken();
            _userRoles = roles;
            _currentRoleTag = MapRolesToDisplay(roles);
        }
        catch
        {
            _userRoles = Array.Empty<string>();
            _currentRoleTag = "Academic Affairs";
        }
    }

    private string HeaderSubtitle
    {
        get
        {
            if (_userRoles.Any(r => string.Equals(r, "AcademicAffairs", StringComparison.OrdinalIgnoreCase)))
                return "EXAM SUITE - ACADEMIC AFFAIRS";
            if (_userRoles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase)))
                return "EXAM SUITE - ADMIN";
            if (_userRoles.Any(r => string.Equals(r, "Lecturer", StringComparison.OrdinalIgnoreCase)))
                return "EXAM SUITE - LECTURER";
            return "EXAM SUITE";
        }
    }

    private static string MapRolesToDisplay(IReadOnlyCollection<string> roles)
    {
        if (roles.Count == 0)
        {
            return "Người dùng";
        }

        var displayNames = roles
            .Select(MapSingleRoleToDisplay)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return displayNames.Count == 0
            ? "Người dùng"
            : string.Join(" & ", displayNames);
    }

    private static string MapSingleRoleToDisplay(string role) => role switch
    {
        "Admin" => "Admin",
        "AcademicAffairs" => "Academic Affairs",
        "Lecturer" => "Lecturer",
        _ => role
    };

    private async Task LoadData()
    {
        _loading = true;
        try
        {
            // Load all required data in parallel
            var loadTasks = new List<Task>
            {
                LoadExamSessions(),
                LoadSubjects(),
                LoadExamRooms()
            };

            if (CanManageMonitor)
            {
                loadTasks.Add(LoadLecturers());
            }
            
            await Task.WhenAll(loadTasks);
            
            // Load ExamSessionSubject
            _examSessionSubject = await ExamSessionSubjectService.GetByIdAsync(Id);
            
            if (_examSessionSubject != null)
            {
                // Backend returns ExamSessionId
                _selectedExamSessionId = _examSessionSubject.ExamSessionId;
                _updateDto.ExamSessionId = _examSessionSubject.ExamSessionId;
                _updateDto.SubjectId = _examSessionSubject.SubjectId;
                _updateDto.Duration = _examSessionSubject.Duration;
                _updateDto.OriginalExamPaperId = _examSessionSubject.OriginalExamPaperId;
                _updateDto.IsCompleted = _examSessionSubject.IsCompleted;
                _updateDto.StartTime = _examSessionSubject.StartTime;
                _updateDto.EndTime = _examSessionSubject.EndTime;
                _updateDto.ExamSessionSubjectCore = _examSessionSubject.ExamSessionSubjectCore;
                _selectedMonitorId = _examSessionSubject.MonitorId;
                _updateDto.MonitorId = _examSessionSubject.MonitorId;
                _selectedExamRoomId = _examSessionSubject.ExamRoomId;
                _updateDto.ExamRoomId = _examSessionSubject.ExamRoomId;
                
                // Nếu có MonitorId nhưng không có MonitorName, tìm trong danh sách giảng viên
                if (_examSessionSubject.MonitorId.HasValue && 
                    string.IsNullOrWhiteSpace(_examSessionSubject.MonitorName) &&
                    _lecturers.Count > 0)
                {
                    var lecturer = _lecturers.FirstOrDefault(l => l.LecturerId == _examSessionSubject.MonitorId.Value);
                    if (lecturer != null)
                    {
                        _examSessionSubject.MonitorName = $"{lecturer.LastName} {lecturer.FirstName}".Trim();
                    }
                }
                
                startTimeString = _examSessionSubject.StartTime.ToString("yyyy-MM-ddTHH:mm");
                if (_examSessionSubject.EndTime.HasValue)
                {
                    endTimeString = _examSessionSubject.EndTime.Value.ToString("yyyy-MM-ddTHH:mm");
                }
                
                // Đảm bảo MudSelect cập nhật sau khi set _selectedMonitorId
                StateHasChanged();
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

    private async Task LoadLecturers()
    {
        _loadingLecturers = true;
        try
        {
            _lecturers = await LecturerService.GetAllLecturersAsync();
            _lecturers = _lecturers
                .OrderBy(l => l.LastName)
                .ThenBy(l => l.FirstName)
                .ThenBy(l => l.LecturerCode)
                .ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi khi tải danh sách giảng viên: {ex.Message}", Severity.Warning);
            _lecturers = new();
        }
        finally
        {
            _loadingLecturers = false;
        }
    }

    private async Task LoadExamRooms()
    {
        _loadingExamRooms = true;
        try
        {
            _examRooms = await ExamRoomService.GetAllAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi khi tải danh sách phòng thi: {ex.Message}", Severity.Warning);
            _examRooms = new();
        }
        finally
        {
            _loadingExamRooms = false;
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
        
        // Validate selected ExamSession
        if (!_selectedExamSessionId.HasValue)
        {
            Snackbar.Add("Vui lòng chọn ca thi", Severity.Error);
            return;
        }
        
        // Validate SubjectId
        if (_updateDto.SubjectId == 0)
        {
            Snackbar.Add("Vui lòng chọn môn học", Severity.Error);
            return;
        }
        
        // Set ExamSessionId, ExamRoomId and MonitorId in update DTO
        _updateDto.ExamSessionId = _selectedExamSessionId.Value;
        _updateDto.ExamRoomId = _selectedExamRoomId;
        _updateDto.MonitorId = _selectedMonitorId;
        
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
            await GoBack();
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

    private string GetLecturerDisplayName(LecturerDto lecturer)
    {
        if (lecturer == null) return string.Empty;
        var fullName = $"{lecturer.LastName} {lecturer.FirstName}".Trim();
        return string.IsNullOrWhiteSpace(fullName)
            ? lecturer.LecturerCode
            : $"{fullName} ({lecturer.LecturerCode})";
    }

    private string GetMonitorDisplayText(int? monitorId)
    {
        if (!monitorId.HasValue)
        {
            return "-- Chưa phân công --";
        }

        var lecturer = _lecturers.FirstOrDefault(l => l.LecturerId == monitorId.Value);
        if (lecturer != null)
        {
            return GetLecturerDisplayName(lecturer);
        }

        // Fallback: nếu không tìm thấy trong danh sách, thử lấy từ MonitorName
        if (_examSessionSubject?.MonitorId == monitorId && !string.IsNullOrWhiteSpace(_examSessionSubject.MonitorName))
        {
            return _examSessionSubject.MonitorName;
        }

        return $"ID: {monitorId}";
    }

    private string GetCurrentMonitorLabel()
    {
        if (_examSessionSubject?.MonitorId == null)
        {
            return "Chưa phân công";
        }

        // Cập nhật MonitorName từ danh sách giảng viên nếu có
        var lecturer = _lecturers.FirstOrDefault(l => l.LecturerId == _examSessionSubject.MonitorId);
        if (lecturer != null)
        {
            // Cập nhật MonitorName để đồng bộ (theo thứ tự Họ Tên)
            var fullName = $"{lecturer.LastName} {lecturer.FirstName}".Trim();
            _examSessionSubject.MonitorName = fullName;
        }

        // Trạng thái chỉ hiển thị "Đã phân công" hoặc "Chưa phân công"
        return "Đã phân công";
    }

    private async Task AssignMonitorAsync()
    {
        if (!CanManageMonitor || !_selectedMonitorId.HasValue || _examSessionSubject == null)
        {
            return;
        }

        _isUpdatingMonitor = true;
        try
        {
            await ExamSessionSubjectService.AssignMonitorAsync(_examSessionSubject.ExamSessionSubjectId, _selectedMonitorId.Value);

            var lecturer = _lecturers.FirstOrDefault(l => l.LecturerId == _selectedMonitorId.Value);
            _examSessionSubject.MonitorId = _selectedMonitorId;
            _examSessionSubject.MonitorName = lecturer != null ? $"{lecturer.LastName} {lecturer.FirstName}" : null;
            _updateDto.MonitorId = _selectedMonitorId;

            Snackbar.Add("Phân công giảng viên giám sát thành công", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi khi phân công giảng viên: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isUpdatingMonitor = false;
        }
    }

    private async Task UnassignMonitorAsync()
    {
        if (!CanManageMonitor || _examSessionSubject == null || _examSessionSubject.MonitorId == null)
        {
            return;
        }

        _isUpdatingMonitor = true;
        try
        {
            await ExamSessionSubjectService.UnassignMonitorAsync(_examSessionSubject.ExamSessionSubjectId);

            _examSessionSubject.MonitorId = null;
            _examSessionSubject.MonitorName = null;
            _selectedMonitorId = null;
            _updateDto.MonitorId = null;

            Snackbar.Add("Đã hủy phân công giảng viên giám sát", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi khi hủy phân công giảng viên: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isUpdatingMonitor = false;
        }
    }

    private async Task GoBack()
    {
        // Kiểm tra role để điều hướng đến dashboard đúng
        var roles = await AuthService.GetUserRolesFromToken();
        if (roles.Contains("Admin"))
        {
            NavigationManager.NavigateTo("/admin/dashboard");
        }
        else if (roles.Contains("AcademicAffairs"))
        {
            NavigationManager.NavigateTo("/academic-affairs/dashboard");
        }
        else
        {
            // Fallback về login nếu không có quyền
            NavigationManager.NavigateTo("/login");
        }
    }
}

