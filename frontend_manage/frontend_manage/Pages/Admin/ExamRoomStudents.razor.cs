using frontend_manage.DTOs;
using frontend_manage.Pages.Admin;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace frontend_manage.Pages.Admin
{
    public partial class ExamRoomStudents : ComponentBase
    {
        [Parameter]
        [SupplyParameterFromQuery]
        public int? examRoomId { get; set; }

        [Parameter]
        [SupplyParameterFromQuery]
        public int? examSessionSubjectId { get; set; }

        [Inject]
        public Api AdminApi { get; set; }

        [Inject]
        public NotificationService NotificationService { get; set; }

        protected List<StudentExamRoomStatusDto>? students;
        protected bool isLoading = true;
        protected string? errorMessage;

        protected string? editingStudentCode;
        void ShowExtraTimeInput(string studentCode)
        {
            editingStudentCode = studentCode;
        }

        protected override async Task OnInitializedAsync()
        {
            if (examRoomId == null || examSessionSubjectId == null)
            {
                errorMessage = "Không tìm thấy mã phòng thi hoặc mã ca thi.";
                isLoading = false;
                return;
            }

            await LoadStudents();

            NotificationService.RoomStatusUpdated += async () =>
            {
                await LoadStudents();
                StateHasChanged();
            };

            await NotificationService.StartAsync();
        }

        protected async Task LoadStudents()
        {
            isLoading = true;
            errorMessage = null;
            try
            {
                students = await AdminApi.GetStudentsByExamRoomAsync(examRoomId.Value, examSessionSubjectId.Value);
            }
            catch (Exception ex)
            {
                errorMessage = $"Lỗi khi tải dữ liệu: {ex.Message}";
            }
            isLoading = false;
        }
        

        private int currentPage = 1;
        private int pageSize = 20;
        private int totalPages => students == null ? 1 : (int)Math.Ceiling((double)students.Count / pageSize);

        private IEnumerable<StudentExamRoomStatusDto> PagedStudents =>
            students == null ? Enumerable.Empty<StudentExamRoomStatusDto>() 
                : students.Skip((currentPage - 1) * pageSize).Take(pageSize);

        private void NextPage()
        {
            if (currentPage < totalPages)
                currentPage++;
        }

        private void PrevPage()
        {
            if (currentPage > 1)
                currentPage--;
        }

        private void GoToPage(int page)
        {
            if (page >= 1 && page <= totalPages)
                currentPage = page;
        }

    }
}
