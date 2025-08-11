using frontend_manage.DTOs;
using frontend_manage.DTOs.AcademicAffairs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using frontend_manage.Services;

namespace frontend_manage.Pages.Monitor
{
    public partial class ExamRoomManagement : ComponentBase, IDisposable
    {
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        [Inject] private AuthService AuthService { get; set; }
        [Inject] private MonitorApi MonitorApi { get; set; }
        [Inject] private NotificationService NotificationService { get; set; }

        private string currentTime = DateTime.Now.ToString("hh:mm:ss tt");
        private Timer? timer;
        private LecturerDto? lecturerInfo;
        private List<LecturerExamRoomDto>? examRooms;
        private int? selectedExamRoomId = null;
        private bool _processing = false;
        private bool _disposed = false;
        

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
                
              
            }
            catch (Exception ex)
            {
                if (!_disposed)
                {
                    lecturerInfo = null;
                    examRooms = null;
                    Console.WriteLine($"Error initializing exam room management: {ex.Message}");
                }
            }

            if (!_disposed)
            {
                currentTime = DateTime.Now.ToString("hh:mm:ss tt");
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
            StateHasChanged();
        }

        public async Task NavigateToDashboard()
        {
            // Chuyển đến trang dashboard của monitor
            Navigation.NavigateTo("/monitor/dashboard");
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






        
    }
}
