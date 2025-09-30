using Microsoft.AspNetCore.Components;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using frontend_manage.Services.AcademicAffairs;
using frontend_manage.DTOs.AcademicAffairs;
using System.Collections.Generic;
using System.Linq;
using Microsoft.JSInterop;
using frontend_manage.Services;

namespace frontend_manage.Pages.AcademicAffairs
{
    public partial class Dashboard : ComponentBase
    {
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private ExamSessionService ExamSessionService { get; set; }
        [Inject] private ExamBatchService ExamBatchService { get; set; }
        [Inject] private ExamSessionSubjectService ExamSessionSubjectService { get; set; }
        [Inject] private AcademicYearService AcademicYearService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }
        [Inject] private AuthService AuthService { get; set; }

        [Parameter] public int Tab { get; set; } = 0;
        private int activeTab = 0;
        private int previousTab = 0;
        private string currentDate = DateTime.Now.ToString("dd/MM/yyyy");
        private string currentTime = DateTime.Now.ToString("HH:mm:ss");
        private Timer timer;

        // Dữ liệu từ API
        private List<ExamSessionDto> examSessions = new();
        private List<ExamBatchDto> examBatches = new();
        private List<ExamSessionSubjectDto> examSessionSubjects = new();
        private List<AcademicYearDto> academicYears = new();

        // Trạng thái loading và error
        private bool isLoading = true;
        private string? errorMessage;

        // Cache để tránh gọi API nhiều lần
        private bool examSessionsLoaded = false;
        private bool examBatchesLoaded = false;
        private bool examSessionSubjectsLoaded = false;
        private bool academicYearsLoaded = false;

        protected override async Task OnInitializedAsync()
        {
            timer = new Timer(_ =>
            {
                currentTime = DateTime.Now.ToString("HH:mm:ss");
                InvokeAsync(StateHasChanged);
            }, null, 0, 1000);

            // Khởi tạo activeTab từ URL parameter
            activeTab = Tab;
            previousTab = Tab;

            // Load dữ liệu cho tab hiện tại
            await LoadTabData(activeTab);
        }

        protected override async Task OnParametersSetAsync()
        {
            // Xử lý khi URL parameter thay đổi
            if (Tab != activeTab)
            {
                previousTab = activeTab;
                activeTab = Tab;
                await LoadTabData(activeTab);
            }
        }

        private async Task ActivateTab(int tabIndex)
        {
            if (activeTab == tabIndex) return;

            previousTab = activeTab;
            activeTab = tabIndex;

            // Cập nhật URL với tab mới
            var url = tabIndex == 0 ? "/academic-affairs/dashboard" : $"/academic-affairs/dashboard/{tabIndex}";
            Navigation.NavigateTo(url, false);

            // Load dữ liệu cho tab mới nếu chưa load
            await LoadTabData(tabIndex);
        }

        private async Task LoadTabData(int tabIndex)
        {
            try
            {
                isLoading = true;
                errorMessage = null;
                StateHasChanged();

                switch (tabIndex)
                {
                    case 0:
                        if (!examSessionsLoaded)
                        {
                            await LoadExamSessions();
                        }
                        break;
                    case 1:
                        if (!examBatchesLoaded)
                        {
                            await LoadExamBatches();
                        }
                        break;
                    case 2:
                        if (!examSessionSubjectsLoaded)
                        {
                            await LoadExamSessionSubjects();
                        }
                        break;
                    case 3:
                        if (!academicYearsLoaded)
                        {
                            await LoadAcademicYears();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Lỗi: {ex.Message}";
                Console.WriteLine($"Error loading tab data: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task LoadExamSessions()
        {
            try
            {
                examSessions = await ExamSessionService.GetAllAsync();
                examSessionsLoaded = true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Lỗi tải danh sách ca thi: {ex.Message}";
                Console.WriteLine($"Error loading exam sessions: {ex.Message}");
            }
        }

        private async Task LoadExamBatches()
        {
            try
            {
                examBatches = await ExamBatchService.GetAllAsync();
                examBatchesLoaded = true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Lỗi tải danh sách đợt thi: {ex.Message}";
                Console.WriteLine($"Error loading exam batches: {ex.Message}");
            }
        }

        private async Task LoadExamSessionSubjects()
        {
            try
            {
                examSessionSubjects = await ExamSessionSubjectService.GetAllAsync();
                examSessionSubjectsLoaded = true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Lỗi tải danh sách môn thi: {ex.Message}";
                Console.WriteLine($"Error loading exam session subjects: {ex.Message}");
            }
        }

        private async Task LoadAcademicYears()
        {
            try
            {
                academicYears = await AcademicYearService.GetAllAsync();
                academicYearsLoaded = true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Lỗi tải danh sách năm học: {ex.Message}";
                Console.WriteLine($"Error loading academic years: {ex.Message}");
            }
        }

        private async Task GoBackToMain()
        {
            try
            {
                Navigation.NavigateTo("/");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error going back to main: {ex.Message}");
                Navigation.NavigateTo("/");
            }
        }

        private async Task Logout()
        {
            try
            {
                await AuthService.Logout();
                Navigation.NavigateTo("/login");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during logout: {ex.Message}");
                Navigation.NavigateTo("/login");
            }
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}
