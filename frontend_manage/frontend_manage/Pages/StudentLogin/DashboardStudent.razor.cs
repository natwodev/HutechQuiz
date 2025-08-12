using frontend_manage.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace frontend_manage.Pages.StudentLogin
{
    public partial class DashboardStudent : ComponentBase, IDisposable
    {
        // Xóa các Inject không cần thiết
        // [Inject] private AuthService AuthService { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        // [Inject] private HttpClient Http { get; set; }
        [Inject] private InfoApi InfoApi { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        private bool isStudent;
        private bool isStudentChecked = false;
        private string currentTime = DateTime.Now.ToString("HH:mm:ss");
        private Timer? timer;
        private StudentInfoDto? studentInfo;
        private List<StudentExamSessionDto>? examSessions;
        private int? selectedExamSessionId = null;
        private bool _processing = false;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var info = await InfoApi.GetStudentProfileAsync();
                if (info != null)
                {
                    studentInfo = new StudentInfoDto
                    {
                        StudentCode = info.StudentCode,
                        FirstName = info.FirstName,
                        LastName = info.LastName
                    };
                }
                else
                {
                    studentInfo = null;
                }

                // Fetch exam sessions
                var allExamSessions = await InfoApi.GetStudentExamSessionsAsync();
                
                // Lọc và sắp xếp để chỉ hiển thị 2 ca thi có ExamSessionStartTime gần nhất
                if (allExamSessions != null && allExamSessions.Count > 0)
                {
                    var now = DateTime.Now;
                    
                    // Sắp xếp theo thời gian gần nhất với thời gian hiện tại
                    examSessions = allExamSessions
                        .OrderBy(session => Math.Abs((session.ExamSessionStartTime - now).TotalMinutes))
                        .Take(2)
                        .ToList();
                }
                else
                {
                    examSessions = new List<StudentExamSessionDto>();
                }
            }
            catch
            {
                studentInfo = null;
                examSessions = new List<StudentExamSessionDto>();
            }
            currentTime = DateTime.Now.ToString("HH:mm:ss");
            timer = new Timer(UpdateTime, null, 0, 1000);
        }

        private void UpdateTime(object? state)
        {
            currentTime = DateTime.Now.ToString("HH:mm:ss");
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            timer?.Dispose();
        }

        private async Task Logout()
        {
            await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "studentInfo");
            await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "studentCode");
            await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "authType");
            Navigation.NavigateTo("/student-login", true);
        }

        private async Task StartExam(StudentExamSessionDto session)
        {
            // Kiểm tra thời gian bắt đầu ca thi
            var now = DateTime.Now;
            if (now < session.ExamSessionStartTime)
            {
                Snackbar.Add($"Chưa đến giờ bắt đầu ca thi. Vui lòng quay lại lúc {session.ExamSessionStartTime:HH:mm dd/MM/yyyy}.", Severity.Warning, config =>
                {
                    config.ShowCloseIcon = true;
                    config.VisibleStateDuration = 4000;
                });
                return;
            }

            // Kiểm tra thời gian quá 15 phút từ ExamSessionStartTime
            var timeLimit = session.ExamSessionStartTime.AddMinutes(15);
            if (now > timeLimit)
            {
                Snackbar.Add($"Đã quá thời gian cho phép làm bài thi. Thời gian bắt đầu: {session.ExamSessionStartTime:HH:mm dd/MM/yyyy}, Thời gian giới hạn: {timeLimit:HH:mm dd/MM/yyyy}.", Severity.Error, config =>
                {
                    config.ShowCloseIcon = true;
                    config.VisibleStateDuration = 6000;
                });
                return;
            }

            // Chuyển hướng sang trang làm bài thi với studentExamSessionId
            Navigation.NavigateTo($"/Exam?studentExamSessionId={session.StudentExamSessionId}");
        }

        private string GetSessionStyle(int sessionId)
        {
            if (selectedExamSessionId == sessionId)
                return "cursor:pointer; background-color:#e3f2fd; box-shadow:0 4px 16px rgba(33,150,243,0.15); border-radius:12px;";
            return "cursor:pointer;";
        }

        private (bool isTooEarly, bool isTooLate, bool isInValidTime) GetTimeStatus(StudentExamSessionDto session)
        {
            var now = DateTime.Now;
            var timeLimit = session.ExamSessionStartTime.AddMinutes(15);
            var isTooEarly = now < session.ExamSessionStartTime;
            var isTooLate = now > timeLimit;
            var isInValidTime = !isTooEarly && !isTooLate;
            
            return (isTooEarly, isTooLate, isInValidTime);
        }

        private string FormatExamTime(DateTime time)
        {
            // Thời gian từ backend đã được lưu theo múi giờ Việt Nam
            // Hiển thị theo múi giờ địa phương của người dùng
            return time.ToString("HH:mm dd/MM/yyyy");
        }

        private async Task ProcessSomething(StudentExamSessionDto session)
        {
            _processing = true;
            StateHasChanged(); // cập nhật giao diện ngay khi bắt đầu xử lý

            // Gọi StartExam logic tại đây
            await StartExam(session);

            _processing = false;
            StateHasChanged(); // cập nhật lại sau khi xử lý xong
        }
    }
}