using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using frontend_manage.Services;
using System.Net.Http.Json;
using FrontEnd.DTOs;
using frontend_manage.Pages.Login;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Text.Json;

namespace frontend_manage.Pages.Login
{
    public partial class DashboardStudent : ComponentBase, IDisposable
    {
        // Xóa các Inject không cần thiết
        // [Inject] private AuthService AuthService { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        // [Inject] private HttpClient Http { get; set; }
        [Inject] private InfoApi InfoApi { get; set; }

        private bool isStudent;
        private bool isStudentChecked = false;
        private string currentTime = DateTime.Now.ToString("hh:mm:ss tt");
        private Timer? timer;
        private StudentInfoDto? studentInfo;
        private List<ExamSessionDto>? examSessions;
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
                examSessions = await InfoApi.GetStudentExamSessionsAsync();
            }
            catch
            {
                studentInfo = null;
                examSessions = null;
            }
            currentTime = DateTime.Now.ToString("hh:mm:ss tt");
            timer = new Timer(UpdateTime, null, 0, 1000);
        }

        private void UpdateTime(object? state)
        {
            currentTime = DateTime.Now.ToString("hh:mm:ss tt");
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
            Navigation.NavigateTo("/student-login", true);
        }

        private async Task StartExam(ExamSessionDto session)
        {
            // Chuyển hướng sang trang làm bài thi với examSessionSubjectId
            Navigation.NavigateTo($"/Exam?examSessionSubjectId={session.ExamSessionSubjectId}");
        }

        private string GetSessionStyle(int sessionId)
        {
            if (selectedExamSessionId == sessionId)
                return "cursor:pointer; background-color:#e3f2fd; box-shadow:0 4px 16px rgba(33,150,243,0.15); border-radius:12px;";
            return "cursor:pointer;";
        }

        private async Task ProcessSomething(ExamSessionDto session)
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