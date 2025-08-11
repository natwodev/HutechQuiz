using frontend_manage.DTOs;
using frontend_manage.DTOs.AcademicAffairs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using frontend_manage.Services;

namespace frontend_manage.Pages.Monitor
{
    public partial class Dashboard : ComponentBase, IDisposable
    {
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        [Inject] private AuthService AuthService { get; set; }
        [Inject] private MonitorApi MonitorApi { get; set; }

        private string currentTime = DateTime.Now.ToString("hh:mm:ss tt");
        private Timer? timer;
        private LecturerDto? lecturerInfo;
        private List<LecturerExamRoomDto>? examRooms;
        private int? selectedExamRoomId = null;
        private bool _processing = false;
        private bool _disposed = false;

        // Thêm các thuộc tính thống kê
        private int totalExamRooms = 0;
        private int ongoingExams = 0;
        private int pendingExams = 0;
        private int completedExams = 0;
        private int totalStudents = 0;

        protected override async Task OnInitializedAsync()
        {
            if (_disposed) return;

            try
            {
                // Kiểm tra xác thực
                if (!await AuthService.IsAuthenticated())
                {
                    Navigation.NavigateTo("/monitor/login", true);
                    return;
                }

                var role = await AuthService.GetUserRoleFromToken();
                if (role != "Lecturer")
                {
                    await AuthService.Logout();
                    Navigation.NavigateTo("/monitor/login", true);
                    return;
                }

                // Lấy thông tin giảng viên từ API
                lecturerInfo = await MonitorApi.GetLecturerInfoAsync();
                if (lecturerInfo == null)
                {
                    // Nếu không lấy được từ API, chuyển về trang login
                    await AuthService.Logout();
                    Navigation.NavigateTo("/monitor/login", true);
                    return;
                }

                // Lấy danh sách phòng thi từ API
                await LoadExamRoomAssignments();
                
                // Tính toán thống kê
                CalculateStatistics();
            }
            catch (Exception ex)
            {
                if (!_disposed)
                {
                    lecturerInfo = null;
                    examRooms = null;
                    Console.WriteLine($"Error initializing dashboard: {ex.Message}");
                }
            }

            if (!_disposed)
            {
                currentTime = DateTime.Now.ToString("hh:mm:ss tt");
                timer = new Timer(UpdateTime, null, 0, 1000);
            }
        }

        private async Task LoadExamRoomAssignments()
        {
            try
            {
                var assignments = await MonitorApi.GetMyAssignmentsAsync();
                if (assignments != null && assignments.Count > 0)
                {
                    // Chuyển đổi từ ExamRoomLecturerAssignmentDto sang LecturerExamRoomDto
                    examRooms = assignments.Select(a => new LecturerExamRoomDto
                    {
                       // ExamRoomLecturerAssignmentId = a.ExamRoomLecturerAssignmentId,
                        ExamRoomId = a.ExamRoomId,
                        RoomName = a.RoomName,
                        SubjectName = a.SubjectName,
                        ExamSessionName = $"Ca thi {a.ExamSessionSubjectId}", // Có thể cần thêm thông tin này từ API
                        StudentCount = a.StudentCount, // Sử dụng StudentCount từ API
                        ExamStartTime = a.StartTime, // Sử dụng StartTime từ API
                        ExamEndTime = a.EndTime, // Sử dụng EndTime từ API
                       // ExamStatus = "pending" // Mặc định là pending
                    }).ToList();
                }
                else
                {
                    examRooms = new List<LecturerExamRoomDto>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading exam room assignments: {ex.Message}");
                examRooms = new List<LecturerExamRoomDto>();
            }
        }

        public async Task RefreshAssignments()
        {
            await LoadExamRoomAssignments();
            CalculateStatistics();
            StateHasChanged();
        }

        private void UpdateTime(object? state)
        {
            if (_disposed) return;
            
            currentTime = DateTime.Now.ToString("hh:mm:ss tt");
            
            // Cập nhật trạng thái phòng thi theo thời gian thực
            UpdateExamRoomStatuses();
            
            InvokeAsync(StateHasChanged);
        }

        // Cập nhật trạng thái phòng thi theo thời gian thực
        private void UpdateExamRoomStatuses()
        {
            if (examRooms == null || examRooms.Count == 0) return;

            var currentTime = DateTime.Now;
            var updated = false;

            foreach (var room in examRooms)
            {
                if (room.ExamStartTime.HasValue && room.ExamEndTime.HasValue)
                {
                    var newStatus = room.ExamStatus;
                    
                    if (currentTime < room.ExamStartTime.Value)
                    {
                        newStatus = "pending";
                    }
                    else if (currentTime >= room.ExamStartTime.Value && currentTime <= room.ExamEndTime.Value)
                    {
                        newStatus = "ongoing";
                    }
                    else
                    {
                        newStatus = "completed";
                    }

                    if (newStatus != room.ExamStatus)
                    {
                        room.ExamStatus = newStatus;
                        updated = true;
                    }
                }
            }

            if (updated)
            {
                CalculateStatistics();
            }
        }

        public void Dispose()
        {
            _disposed = true;
            timer?.Dispose();
        }

        private async Task Logout()
        {
            _disposed = true;
            timer?.Dispose();
            await AuthService.Logout();
            Navigation.NavigateTo("/monitor/login", true);
        }

        private async Task StartMonitoring(LecturerExamRoomDto room)
        {
            _processing = true;
            StateHasChanged();

            try
            {
                // TODO: Chuyển hướng sang trang giám sát phòng thi
                // Navigation.NavigateTo($"/monitor/room/{room.ExamRoomId}");
                
                // Tạm thời hiển thị thông báo
                await JSRuntime.InvokeVoidAsync("alert", $"Bắt đầu giám sát phòng thi: {room.RoomName}");
            }
            catch (Exception ex)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Lỗi: {ex.Message}");
            }
            finally
            {
                _processing = false;
                StateHasChanged();
            }
        }

        private string GetRoomStyle(int roomId)
        {
            if (selectedExamRoomId == roomId)
                return "cursor:pointer; background-color:#e3f2fd; box-shadow:0 4px 16px rgba(33,150,243,0.15); border-radius:12px;";
            return "cursor:pointer;";
        }

        private string GetRoomStatus(string status)
        {
            return status switch
            {
                "ongoing" => "Đang hoạt động",
                "pending" => "Chờ bắt đầu",
                "completed" => "Đã hoàn thành",
                "cancelled" => "Đã hủy",
                _ => status
            };
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "ongoing" => "#4caf50",
                "pending" => "#ff9800",
                "completed" => "#2196f3",
                "cancelled" => "#f44336",
                _ => "#757575"
            };
        }

        private void CalculateStatistics()
        {
            if (examRooms == null || examRooms.Count == 0)
            {
                totalExamRooms = 0;
                ongoingExams = 0;
                pendingExams = 0;
                completedExams = 0;
                totalStudents = 0;
                return;
            }

            totalExamRooms = examRooms.Count;
            ongoingExams = examRooms.Count(r => r.ExamStatus == "ongoing");
            pendingExams = examRooms.Count(r => r.ExamStatus == "pending");
            completedExams = examRooms.Count(r => r.ExamStatus == "completed");
            totalStudents = examRooms.Sum(r => r.StudentCount);
        }
    }
}
