using Microsoft.AspNetCore.Components;
using MudBlazor;
using frontend_manage.DTOs;
using frontend_manage.Services;
using System.Collections.Generic;
using System.Linq;

namespace frontend_manage.Pages.Monitor.Components;

public partial class ActivityMonitorTab : ComponentBase, IDisposable
{
    [Inject] private StudentActivityService ActivityService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    [Parameter] public int? ExamSessionSubjectId { get; set; }

    private List<StudentActivityDto> Activities { get; set; } = new();
    private bool isLoading = false;
    private string? errorMessage;
    private string searchText = string.Empty;

    private bool _signalRHandlersBound = false;
    
    // Dictionary để lưu số lần vi phạm của từng sinh viên
    private Dictionary<string, int> _violationCounts = new();
    private Dictionary<string, List<StudentActivityDto>> _studentViolations = new();
    private Dictionary<string, string> _studentNames = new(); // StudentCode -> StudentName
    private const int MAX_VIOLATIONS = 3;
    
    // Các loại vi phạm nghiêm trọng
    private readonly HashSet<string> _violationTypes = new()
    {
        "TabSwitch", "FullscreenExit", "Copy", "Paste", 
        "RightClick", "DevTools", "Screenshot",
        "AppBackground", "AppSwitch"
    };
    
    // Class để nhóm activities theo sinh viên
    private class StudentViolationSummary
    {
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public int ViolationCount { get; set; }
        public List<StudentActivityDto> Violations { get; set; } = new();
    }

    protected override async Task OnInitializedAsync()
    {
        if (ExamSessionSubjectId.HasValue)
        {
            await LoadActivities();
            await InitializeSignalR();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ExamSessionSubjectId.HasValue && Activities.Count == 0)
        {
            await LoadActivities();
        }
    }

    private async Task InitializeSignalR()
    {
        if (_signalRHandlersBound) return;

        try
        {
            NotificationService.OnStudentActivityDetected += OnStudentActivityDetected;
            _signalRHandlersBound = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing SignalR: {ex.Message}");
        }
    }

    private void OnStudentActivityDetected(object data)
    {
        try
        {
            // Parse data từ SignalR
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            var activityData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (activityData != null)
            {
                var newActivity = new StudentActivityDto
                {
                    StudentActivityId = activityData.ContainsKey("activityId") 
                        ? Convert.ToInt32(activityData["activityId"].ToString()) 
                        : 0,
                    StudentCode = activityData.ContainsKey("studentCode") 
                        ? activityData["studentCode"].ToString() ?? string.Empty 
                        : string.Empty,
                    StudentName = activityData.ContainsKey("studentName") 
                        ? activityData["studentName"].ToString() 
                        : null,
                    ActivityType = activityData.ContainsKey("activityType") 
                        ? activityData["activityType"].ToString() ?? string.Empty 
                        : string.Empty,
                    Description = activityData.ContainsKey("description") 
                        ? activityData["description"].ToString() 
                        : null,
                    ActivityTime = activityData.ContainsKey("activityTime") 
                        ? DateTime.Parse(activityData["activityTime"].ToString() ?? DateTime.Now.ToString()) 
                        : DateTime.Now
                };

                // Thêm vào đầu danh sách
                Activities.Insert(0, newActivity);

                // Lưu tên sinh viên
                if (!string.IsNullOrEmpty(newActivity.StudentName))
                {
                    _studentNames[newActivity.StudentCode] = newActivity.StudentName;
                }
                
                // Cập nhật số lần vi phạm
                if (_violationTypes.Contains(newActivity.ActivityType))
                {
                    if (!_violationCounts.ContainsKey(newActivity.StudentCode))
                    {
                        _violationCounts[newActivity.StudentCode] = 0;
                        _studentViolations[newActivity.StudentCode] = new List<StudentActivityDto>();
                    }
                    
                    _violationCounts[newActivity.StudentCode]++;
                    _studentViolations[newActivity.StudentCode].Insert(0, newActivity);
                }

                // Giới hạn số lượng để tránh quá tải
                if (Activities.Count > 1000)
                {
                    Activities = Activities.Take(1000).ToList();
                }

                InvokeAsync(StateHasChanged);

                // Hiển thị thông báo
                Snackbar.Add(
                    $"Phát hiện hành động: {GetActivityTypeDisplayName(newActivity.ActivityType)} từ {newActivity.StudentCode}",
                    Severity.Info);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing SignalR activity: {ex.Message}");
        }
    }

    private async Task LoadActivities()
    {
        if (!ExamSessionSubjectId.HasValue) return;

        try
        {
            isLoading = true;
            errorMessage = null;

            Activities = await ActivityService.GetActivitiesByExamSessionAsync(ExamSessionSubjectId.Value);
            
            // Tính toán số lần vi phạm cho từng sinh viên
            CalculateViolationCounts();
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            errorMessage = $"Lỗi khi tải danh sách hành động: {ex.Message}";
            Console.WriteLine($"Error loading activities: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void CalculateViolationCounts()
    {
        _violationCounts.Clear();
        _studentViolations.Clear();
        _studentNames.Clear();

        foreach (var activity in Activities)
        {
            // Lưu tên sinh viên
            if (!string.IsNullOrEmpty(activity.StudentName))
            {
                _studentNames[activity.StudentCode] = activity.StudentName;
            }
            
            // Chỉ đếm các vi phạm nghiêm trọng
            if (_violationTypes.Contains(activity.ActivityType))
            {
                if (!_violationCounts.ContainsKey(activity.StudentCode))
                {
                    _violationCounts[activity.StudentCode] = 0;
                    _studentViolations[activity.StudentCode] = new List<StudentActivityDto>();
                }
                
                _violationCounts[activity.StudentCode]++;
                _studentViolations[activity.StudentCode].Add(activity);
            }
        }
    }

    private int GetViolationCount(string studentCode)
    {
        return _violationCounts.TryGetValue(studentCode, out var count) ? count : 0;
    }

    private string GetViolationStatus(string studentCode)
    {
        var count = GetViolationCount(studentCode);
        if (count >= MAX_VIOLATIONS)
            return "Đạt ngưỡng";
        if (count > 0)
            return "Cảnh báo";
        return "Bình thường";
    }

    private string GetViolationStatusClass(string studentCode)
    {
        var count = GetViolationCount(studentCode);
        if (count >= MAX_VIOLATIONS)
            return "violation-status-danger";
        if (count > 0)
            return "violation-status-warning";
        return "violation-status-safe";
    }

    private string GetViolationStatusEmoji(string studentCode)
    {
        var count = GetViolationCount(studentCode);
        if (count >= MAX_VIOLATIONS)
            return "🔴";
        if (count > 0)
            return "🟡";
        return "🟢";
    }

    private async Task ViewViolationDetails(string studentCode, string studentName)
    {
        var violations = _studentViolations.TryGetValue(studentCode, out var list) 
            ? list.OrderByDescending(v => v.ActivityTime).ToList()
            : new List<StudentActivityDto>();

        var violationCount = GetViolationCount(studentCode);

        // Tạo dialog với parameters
        var parameters = new DialogParameters<ViolationDetailsDialog>
        {
            { x => x.StudentCode, studentCode },
            { x => x.StudentName, studentName },
            { x => x.ViolationCount, violationCount },
            { x => x.MaxViolations, MAX_VIOLATIONS },
            { x => x.Violations, violations }
        };
        
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseOnEscapeKey = true,
            CloseButton = true
        };

        await DialogService.ShowAsync<ViolationDetailsDialog>(
            $"Chi tiết vi phạm - {studentCode}",
            parameters,
            options);
    }

    private async Task RefreshActivities()
    {
        await LoadActivities();
        Snackbar.Add("Đã làm mới danh sách hành động", Severity.Success);
    }

    private void PerformSearch()
    {
        StateHasChanged();
    }

    private void ClearFilter()
    {
        searchText = string.Empty;
        StateHasChanged();
    }

    // Trả về danh sách sinh viên đã được nhóm (mỗi sinh viên 1 dòng)
    private List<StudentViolationSummary> FilteredStudents
    {
        get
        {
            // Lấy tất cả sinh viên có activities (bao gồm cả vi phạm và không vi phạm)
            var allStudentCodes = Activities
                .Select(a => a.StudentCode)
                .Distinct()
                .ToList();

            var students = allStudentCodes.Select(studentCode => new StudentViolationSummary
            {
                StudentCode = studentCode,
                StudentName = _studentNames.TryGetValue(studentCode, out var name) 
                    ? name 
                    : studentCode,
                ViolationCount = GetViolationCount(studentCode),
                Violations = _studentViolations.TryGetValue(studentCode, out var violations)
                    ? violations.OrderByDescending(v => v.ActivityTime).ToList()
                    : new List<StudentActivityDto>()
            }).ToList();

            // Lọc theo search text
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var searchLower = searchText.ToLower();
                students = students.Where(s =>
                    s.StudentCode.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    s.StudentName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Sắp xếp: vi phạm nhiều nhất trước, sau đó theo mã SV
            return students
                .OrderByDescending(s => s.ViolationCount)
                .ThenBy(s => s.StudentCode)
                .ToList();
        }
    }

    private string GetActivityTypeDisplayName(string activityType)
    {
        return activityType switch
        {
            "ScreenBlur" => "Thoát màn hình",
            "Screenshot" => "Chụp màn hình",
            "TabSwitch" => "Chuyển tab",
            "Copy" => "Sao chép",
            "Paste" => "Dán",
            "RightClick" => "Click chuột phải",
            "DevTools" => "Mở DevTools",
            "FullscreenExit" => "Thoát toàn màn hình",
            "AppBackground" => "Ứng dụng vào nền",
            "AppForeground" => "Ứng dụng lên trước",
            "AppSwitch" => "Chuyển ứng dụng",
            _ => activityType
        };
    }

    private string GetActivityIcon(string activityType)
    {
        return activityType switch
        {
            "ScreenBlur" => Icons.Material.Filled.VisibilityOff,
            "Screenshot" => Icons.Material.Filled.CameraAlt,
            "TabSwitch" => Icons.Material.Filled.Tab,
            "Copy" => Icons.Material.Filled.ContentCopy,
            "Paste" => Icons.Material.Filled.ContentPaste,
            "RightClick" => Icons.Material.Filled.Mouse,
            "DevTools" => Icons.Material.Filled.Code,
            "FullscreenExit" => Icons.Material.Filled.FullscreenExit,
            "AppBackground" => Icons.Material.Filled.PhoneAndroid,
            "AppForeground" => Icons.Material.Filled.PhoneIphone,
            "AppSwitch" => Icons.Material.Filled.SwapHoriz,
            _ => Icons.Material.Filled.Warning
        };
    }

    private Color GetActivityTypeColor(string activityType)
    {
        return activityType switch
        {
            "ScreenBlur" => Color.Warning,
            "Screenshot" => Color.Error,
            "TabSwitch" => Color.Warning,
            "Copy" => Color.Error,
            "Paste" => Color.Error,
            "RightClick" => Color.Info,
            "DevTools" => Color.Error,
            "FullscreenExit" => Color.Warning,
            "AppBackground" => Color.Error,
            "AppForeground" => Color.Info,
            "AppSwitch" => Color.Error,
            _ => Color.Default
        };
    }

    private string GetActivityTypeClass(string activityType)
    {
        return activityType switch
        {
            "ScreenBlur" => "activity-warning",
            "Screenshot" => "activity-danger",
            "TabSwitch" => "activity-warning",
            "Copy" => "activity-danger",
            "Paste" => "activity-danger",
            "RightClick" => "activity-info",
            "DevTools" => "activity-danger",
            "FullscreenExit" => "activity-warning",
            "AppBackground" => "activity-danger",
            "AppForeground" => "activity-info",
            "AppSwitch" => "activity-danger",
            _ => "activity-default"
        };
    }


    public void Dispose()
    {
        if (_signalRHandlersBound)
        {
            NotificationService.OnStudentActivityDetected -= OnStudentActivityDetected;
        }
    }
}

