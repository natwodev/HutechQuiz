using System.Text.Json;
using frontend_manage.DTOs;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace frontend_manage.Pages.Monitor;

public partial class Dashboard
{
    // Service injections are handled in the Razor file

    private string? userName;
    private bool loading = true;
    private string? searchTerm;

    // Mock data
    private List<StudentExamDto>? students;

    protected override async Task OnInitializedAsync()
    {
        // TEMPORARILY COMMENTED OUT FOR UI TESTING
        // Authentication checks will be re-enabled after UI testing is complete
        /*
        // Check if user is authenticated
        if (!await AuthService.IsAuthenticated())
        {
            Navigation.NavigateTo("/monitor/login");
            return;
        }

        // Check if user has monitor access
        var role = await AuthService.GetUserRoleFromToken();
        if (!(role == "Admin" || role == "ITManager" || role == "ExamManager"))
        {
            Navigation.NavigateTo("/monitor/login");
            return;
        }
        */

        // Set default user name for UI testing
        userName = "Monitor (Testing Mode)";
        
        // TEMPORARILY COMMENTED OUT FOR UI TESTING
        /*
        // Get user name
        var userJson = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "cookieAuth");
        if (!string.IsNullOrEmpty(userJson))
        {
            try
            {
                using var document = JsonDocument.Parse(userJson);
                if (document.RootElement.TryGetProperty("userName", out var userNameElement))
                {
                    userName = userNameElement.GetString();
                }
            }
            catch
            {
                userName = "Monitor";
            }
        }
        else
        {
            userName = "Monitor";
        }
        */

        // Initialize mock data
        await LoadData();
    }

    private async Task LoadData()
    {
        loading = true;
        // Simulate API call with delay
        await Task.Delay(800);

        // Mock student data with a range of scores for testing pass/fail coloring
        students = new List<StudentExamDto>
        {
            new StudentExamDto { StudentCode = "001", FullName = "LƯU THỊ LAN ANH", ExamStatus = "Đã nộp bài", IsLoggedIn = true, ExtraTime = 0, Score = 1.2M },
            new StudentExamDto { StudentCode = "002", FullName = "TRẦN TUẤN ANH", ExamStatus = "Đang thi", IsLoggedIn = true, ExtraTime = 0, Score = null },
            new StudentExamDto { StudentCode = "003", FullName = "TRẦN THỊ HỒNG ANH", ExamStatus = "Chưa thi hoặc bỏ thi", IsLoggedIn = false, ExtraTime = 0, Score = null },
            new StudentExamDto { StudentCode = "004", FullName = "NGÔ THỊ XUÂN BẢU", ExamStatus = "Đã nộp bài", IsLoggedIn = true, ExtraTime = 0, Score = 4.5M },
            new StudentExamDto { StudentCode = "005", FullName = "NGUYỄN THỊ THANH BÌNH", ExamStatus = "Đang thi", IsLoggedIn = true, ExtraTime = 5, Score = null },
            new StudentExamDto { StudentCode = "006", FullName = "NGÔ VĂN BÌNH", ExamStatus = "Đã nộp bài", IsLoggedIn = true, ExtraTime = 0, Score = 8.5M },
            new StudentExamDto { StudentCode = "007", FullName = "LÊ VĂN CƯỜNG", ExamStatus = "Đã nộp bài", IsLoggedIn = true, ExtraTime = 0, Score = 7.0M },
            new StudentExamDto { StudentCode = "008", FullName = "TRẦN THỊ DIỆP", ExamStatus = "Đang thi", IsLoggedIn = true, ExtraTime = 10, Score = null },
            new StudentExamDto { StudentCode = "009", FullName = "PHẠM MINH ĐỨC", ExamStatus = "Đã nộp bài", IsLoggedIn = true, ExtraTime = 0, Score = 3.8M },
            new StudentExamDto { StudentCode = "010", FullName = "VŨ HOÀNG HÀ", ExamStatus = "Đã nộp bài", IsLoggedIn = true, ExtraTime = 0, Score = 9.0M },
            new StudentExamDto { StudentCode = "011", FullName = "LÊ VĂN HIỀN", ExamStatus = "Đã nộp bài", IsLoggedIn = true, ExtraTime = 0, Score = 5.0M },
            new StudentExamDto { StudentCode = "012", FullName = "NGUYỄN THỊ HƯƠNG", ExamStatus = "Đã nộp bài", IsLoggedIn = true, ExtraTime = 0, Score = 6.5M },
            new StudentExamDto { StudentCode = "013", FullName = "TRỊNH VĂN KHANG", ExamStatus = "Đang thi", IsLoggedIn = true, ExtraTime = 5, Score = null },
            new StudentExamDto { StudentCode = "014", FullName = "PHAN THỊ LINH", ExamStatus = "Đã nộp bài", IsLoggedIn = true, ExtraTime = 0, Score = 4.0M },
            new StudentExamDto { StudentCode = "015", FullName = "VŨ QUANG MINH", ExamStatus = "Đã nộp bài", IsLoggedIn = true, ExtraTime = 0, Score = 7.5M },
            new StudentExamDto { StudentCode = "016", FullName = "LÊ THỊ NGỌC", ExamStatus = "Đã nộp bài", IsLoggedIn = true, ExtraTime = 0, Score = 8.0M },
            new StudentExamDto { StudentCode = "017", FullName = "NGUYỄN TRUNG HIẾU", ExamStatus = "Chưa thi hoặc bỏ thi", IsLoggedIn = false, ExtraTime = 0, Score = null },
            new StudentExamDto { StudentCode = "018", FullName = "HOÀNG MINH PHƯƠNG", ExamStatus = "Đã nộp bài", IsLoggedIn = true, ExtraTime = 0, Score = 2.5M },
            new StudentExamDto { StudentCode = "019", FullName = "PHẠM QUỐC TUẤN", ExamStatus = "Đang thi", IsLoggedIn = true, ExtraTime = 10, Score = null },
            new StudentExamDto { StudentCode = "020", FullName = "TRẦN THANH THÚY", ExamStatus = "Đã nộp bài", IsLoggedIn = true, ExtraTime = 0, Score = 9.5M },
        };

        loading = false;
    }

    private async Task Search()
    {
        loading = true;
        // Simulate search API call
        await Task.Delay(500);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            students = students?.Where(s => 
                s.StudentCode.ToLower().Contains(searchTerm) || 
                s.FullName.ToLower().Contains(searchTerm)
            ).ToList();
        }
        else
        {
            // Reset to original data
            await LoadData();
        }

        loading = false;
    }

    private async Task RefreshData()
    {
        await LoadData();
    }

    private async Task Logout()
    {
        await AuthService.Logout();
        Navigation.NavigateTo("/monitor/login");
    }
    
    // Không còn cần phương thức DisposeAsync vì đã loại bỏ IAsyncDisposable
}

// DTO class for student exam data
public class StudentExamDto
{
    public string StudentCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string ExamStatus { get; set; } = "";
    public bool IsLoggedIn { get; set; }
    public int ExtraTime { get; set; }
    public decimal? Score { get; set; }
}
