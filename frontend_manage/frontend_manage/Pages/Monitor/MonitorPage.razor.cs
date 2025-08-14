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

        [Parameter]
        [SupplyParameterFromQuery]
        public int? ExamRoomId { get; set; }

        [Parameter]
        [SupplyParameterFromQuery]
        public int? ExamSessionSubjectId { get; set; }

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

            // Nếu có ExamRoomId, gọi API để lấy dữ liệu
            if (ExamRoomId.HasValue)
            {
                await LoadExamData();
            }
        }

        private async Task LoadExamData()
        {
            if (!ExamRoomId.HasValue) return;

            try
            {
                isLoading = true;
                errorMessage = null;
                
                // Gọi API để lấy dữ liệu sinh viên và thông tin môn thi
                examData = await MonitorApi.GetStudentsByExamRoomAsync(ExamRoomId.Value, ExamSessionSubjectId ?? 0);
                
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
    }
}
