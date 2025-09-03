using frontend_manage.DTOs;
using frontend_manage.DTOs.AcademicAffairs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using frontend_manage.Services;

namespace frontend_manage.Pages.Monitor
{
    public partial class ExamRoomManagement : BaseComponent, IDisposable
    {
        // [Inject] private NavigationManager Navigation { get; set; }
        // [Inject] private IJSRuntime JSRuntime { get; set; }
        [Inject] private AuthService AuthService { get; set; }
        [Inject] private MonitorService MonitorService { get; set; }
        [Inject] private NotificationService NotificationService { get; set; }

        private string currentTime = DateTime.Now.ToString("hh:mm:ss tt");
        private Timer? timer;
        private LecturerDto? lecturerInfo;
        private List<SubjectExamRoomStatusDto>? examRooms;
        private int? selectedExamRoomId = null;
        private int? selectedExamSessionSubjectId = null;
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
                lecturerInfo = await MonitorService.GetLecturerInfoAsync();
                if (lecturerInfo == null)
                {
                    // Nếu không lấy được từ API, chuyển về trang login
                    await AuthService.Logout();
                    Navigation.NavigateTo("/monitor/login", true);
                    return;
                }

                // Lấy danh sách phòng thi từ API
                var assignments = await MonitorService.GetMyAssignmentsAsync();
                if (assignments != null && assignments.Count > 0)
                {
                    examRooms = assignments;
                }
                else
                {
                    examRooms = new List<SubjectExamRoomStatusDto>();
                }
                
              
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





        public async Task NavigateToDashboard()
        {
            if (selectedExamSessionSubjectId.HasValue)
            {
                // Lưu dữ liệu vào session storage thay vì truyền qua URL
                await JSRuntime.InvokeVoidAsync("sessionStorage.setItem", "examSessionSubjectId", selectedExamSessionSubjectId.Value.ToString());
                
                // Chuyển đến trang monitor không có tham số
                Navigation.NavigateTo("/monitor");
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
            
            try
            {
                // Xóa dữ liệu khỏi session storage khi đăng xuất
                await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", "examSessionSubjectId");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing session storage: {ex.Message}");
            }
            
            await AuthService.Logout();
            Navigation.NavigateTo("/monitor/login", true);
        }






        
    }
}