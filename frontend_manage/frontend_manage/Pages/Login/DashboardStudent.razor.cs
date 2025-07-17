using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using frontend_manage.Services;
using System.Net.Http.Json;
using FrontEnd.DTOs;
using frontend_manage.Pages.Login;

namespace frontend_manage.Pages.Login
{
    public partial class DashboardStudent : ComponentBase, IDisposable
    {
        [Inject] private AuthService AuthService { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private HttpClient Http { get; set; } // Thêm dòng này
        [Inject] private InfoApi InfoApi { get; set; } // Inject InfoApi

        private bool isStudent;
        private bool isStudentChecked = false;
        private string currentTime = DateTime.Now.ToString("hh:mm:ss tt");
        private Timer? timer;
        private StudentInfoDto? studentInfo;

        protected override async Task OnInitializedAsync()
        {
            var role = await AuthService.GetUserRoleFromToken();
            isStudent = role == "Student";
            isStudentChecked = true;
            if (!isStudent)
            {
                Navigation.NavigateTo("/student-login");
                return;
            }
            var localStudentInfo = await AuthService.GetStudentInfoAsync();
            Console.WriteLine($"[DEBUG] localStudentInfo: {localStudentInfo?.StudentCode}");
            if (localStudentInfo != null && !string.IsNullOrEmpty(localStudentInfo.StudentCode))
            {
                studentInfo = await InfoApi.GetStudentInfoByCodeAsync(localStudentInfo.StudentCode);
                Console.WriteLine($"[DEBUG] studentInfo: {studentInfo?.StudentCode} {studentInfo?.FirstName} {studentInfo?.LastName}");
            }
            else
            {
                Console.WriteLine("[DEBUG] Không lấy được studentCode từ localStorage!");
            }
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
            await AuthService.Logout();
        }
    }
}