using Microsoft.AspNetCore.Components;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using frontend_manage.Services;
using frontend_manage.DTOs;
using System.Collections.Generic;
using System.Linq;
using Microsoft.JSInterop;

namespace frontend_manage.Pages.Monitor
{
    public partial class MonitorPage : ComponentBase
    {
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private MonitorService MonitorService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        [Inject] private NotificationService NotificationService { get; set; }

        // Không còn sử dụng query string parameters
        private int? ExamSessionSubjectId { get; set; }

        private int activeTab = 0;
        private int previousTab = 0;
        // Dữ liệu từ API
        private StudentListResponse? examData;
        private bool isLoading = true;
        private string? errorMessage;

        // SignalR Connection Status
        private bool isSignalRConnected = false;
        private string lastNotification = string.Empty;
        private Timer notificationTimer;

        // Tránh đăng ký event trùng lặp khi re-render
        private bool _signalRHandlersBound = false;
        private bool _signalRInitialized = false;

        protected override async Task OnInitializedAsync()
        {
            // Timer để tự động ẩn thông báo sau 5 giây
            notificationTimer = new Timer(_ =>
            {
                if (!string.IsNullOrEmpty(lastNotification))
                {
                    InvokeAsync(() =>
                    {
                        lastNotification = string.Empty;
                        StateHasChanged();
                    });
                }
            }, null, 5000, 5000);

            // Đọc dữ liệu từ session storage thay vì query string
            await LoadDataFromSessionStorage();
        }

        private async Task LoadDataFromSessionStorage()
        {
            try
            {
                var examSessionSubjectIdStr = await JSRuntime.InvokeAsync<string>("sessionStorage.getItem", "examSessionSubjectId");

                if (!string.IsNullOrEmpty(examSessionSubjectIdStr) && int.TryParse(examSessionSubjectIdStr, out int examSessionSubjectId))
                {
                    ExamSessionSubjectId = examSessionSubjectId;

                    // Gọi API để lấy dữ liệu ban đầu
                    await LoadExamData();
                    
                    // Khởi tạo SignalR ngay sau khi có ExamSessionSubjectId
                    await InitializeSignalR();
                }
                else
                {
                    Navigation.NavigateTo("/monitor/exam-room-management", true);
                }
            }
            catch (Exception ex)
            {
                Navigation.NavigateTo("/monitor/exam-room-management", true);
            }
        }

        private async Task LoadExamData()
        {
            if (!ExamSessionSubjectId.HasValue) return;

            try
            {
                isLoading = true;
                errorMessage = null;

                examData = await MonitorService.GetStudentsByExamRoomAsync(ExamSessionSubjectId.Value);

                if (examData == null)
                {
                    errorMessage = "Không thể tải dữ liệu từ server";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Lỗi: {ex.Message}";
                Console.WriteLine($"Error loading exam data: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private void ActivateTab(int tabIndex)
        {
            previousTab = activeTab;
            activeTab = tabIndex;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Không làm gì để tránh re-render loop
        }

        private async Task InitializeSignalR()
        {
            if (!ExamSessionSubjectId.HasValue || _signalRInitialized)
            {
                return;
            }

            try
            {
                _signalRInitialized = true;
                
                // Khởi tạo SignalR connection
                await NotificationService.StartAsync();

                if (!_signalRHandlersBound)
                {
                    NotificationService.OnConnectionStateChanged += OnConnectionStateChanged;
                    NotificationService.OnRoomStatusUpdated += OnRoomStatusUpdated;
                    _signalRHandlersBound = true;
                }

                // Tham gia nhóm giám sát
                await NotificationService.JoinLecturerView(ExamSessionSubjectId.Value);

                // Cập nhật trạng thái kết nối
                isSignalRConnected = true;
            }
            catch (Exception ex)
            {
                _signalRInitialized = false; // Reset để có thể retry
                isSignalRConnected = false;
            }
        }

        private void OnConnectionStateChanged(bool isConnected)
        {
            isSignalRConnected = isConnected;
            InvokeAsync(StateHasChanged);
        }

        private void OnRoomStatusUpdated(StudentListResponse response)
        {
            try
            {

                _ = InvokeAsync(() =>
                {
                    if (examData == null)
                        examData = new StudentListResponse();

                    // ⚡ clone list để đổi reference
                    examData.Students = response.Students?.ToList() ?? new List<StudentExamRoomStatusDto>();
                    examData.Subject = response.Subject;

                    StateHasChanged(); // refresh MonitorPage + StudentListTab
                });
            }
            catch (Exception ex)
            {

                // fallback: gọi API lại
                _ = InvokeAsync(async () =>
                {
                    await LoadExamData();
                    StateHasChanged();
                });
            }
        }


        private void ShowNotification(string message, bool isError = false)
        {
            lastNotification = message;
            InvokeAsync(StateHasChanged);

            _ = Task.Delay(5000).ContinueWith(_ =>
            {
                InvokeAsync(() =>
                {
                    lastNotification = string.Empty;
                    StateHasChanged();
                });
            });
        }

        private void ClearNotification()
        {
            lastNotification = string.Empty;
            StateHasChanged();
        }

        public void Dispose()
        {
            if (NotificationService != null)
            {
                NotificationService.OnRoomStatusUpdated -= OnRoomStatusUpdated;
                NotificationService.OnConnectionStateChanged -= OnConnectionStateChanged;
            }

            if (ExamSessionSubjectId.HasValue)
            {
                _ = NotificationService?.LeaveLecturerView(ExamSessionSubjectId.Value);
            }

            _ = NotificationService?.StopAsync();

            notificationTimer?.Dispose();
        }

        private async Task GoBackToExamRoomManagement()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", "examSessionSubjectId");
                Navigation.NavigateTo("/monitor/exam-room-management");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error going back to exam room management: {ex.Message}");
                Navigation.NavigateTo("/monitor/exam-room-management");
            }
        }
    }
}
