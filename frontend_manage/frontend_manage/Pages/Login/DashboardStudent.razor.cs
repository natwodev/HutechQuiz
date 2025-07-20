using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using frontend_manage.Services;
using System.Net.Http.Json;
using FrontEnd.DTOs;
using frontend_manage.Pages.Login;
using Microsoft.JSInterop;

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
            }
            catch
            {
                studentInfo = null;
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
    }
}