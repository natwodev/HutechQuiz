using Microsoft.AspNetCore.Components;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using frontend_manage.Services;
using frontend_manage.DTOs;
using System.Collections.Generic;
using Microsoft.JSInterop;

namespace frontend_manage.Pages.Monitor
{
    public partial class MonitorPage : ComponentBase, IDisposable
    {
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private MonitorApi MonitorApi { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }

        // Không còn sử dụng query string parameters
        private int? ExamSessionSubjectId { get; set; }

        private int activeTab = 0;
        private int previousTab = 0;
        private string currentDate = DateTime.Now.ToString("dd/MM/yyyy");
        private string currentTime = DateTime.Now.ToString("HH:mm:ss");
        private Timer timer;

        // Dữ liệu từ API
        private StudentListResponse? examData;
        private bool isLoading = true;
        private string? errorMessage;

        protected override async Task OnInitializedAsync()
        {
            timer = new Timer(_ =>
            {
                currentTime = DateTime.Now.ToString("HH:mm:ss");
                // Cập nhật UI mỗi giây để hiển thị thời gian còn lại
                InvokeAsync(StateHasChanged);
            }, null, 0, 1000);

            // Đọc dữ liệu từ session storage thay vì query string
            await LoadDataFromSessionStorage();
        }

        private async Task LoadDataFromSessionStorage()
        {
            try
            {
                // Đọc dữ liệu từ session storage
                var examSessionSubjectIdStr = await JSRuntime.InvokeAsync<string>("sessionStorage.getItem", "examSessionSubjectId");

                if (!string.IsNullOrEmpty(examSessionSubjectIdStr) && int.TryParse(examSessionSubjectIdStr, out int examSessionSubjectId))
                {
                    ExamSessionSubjectId = examSessionSubjectId;
                    
                    // Gọi API để lấy dữ liệu
                    await LoadExamData();
                }
                else
                {
                    // Nếu không có dữ liệu trong session storage, chuyển về trang exam room management
                    // Điều này ngăn người dùng truy cập trực tiếp vào /monitor
                    Navigation.NavigateTo("/monitor/exam-room-management", true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data from session storage: {ex.Message}");
                // Nếu có lỗi, chuyển về trang exam room management
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
                
                // Gọi API để lấy dữ liệu sinh viên và thông tin môn thi
                examData = await MonitorApi.GetStudentsByExamRoomAsync(ExamSessionSubjectId.Value);
                
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

        public void Dispose()
        {
            timer?.Dispose();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Không cần gọi LoadDataFromSessionStorage ở đây nữa vì đã gọi trong OnInitializedAsync
        }

        private async Task GoBackToExamRoomManagement()
        {
            try
            {
                // Xóa dữ liệu khỏi session storage
                await JSRuntime.InvokeVoidAsync("sessionStorage.removeItem", "examSessionSubjectId");
                
                // Chuyển về trang exam room management
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
