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
            await NotificationService.JoinRoom(examRoomId.Value);
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

        protected async Task ResetLogin(string studentCode)
        {
            try
            {
                var result = await AdminApi.ActiveLoginAsync(studentCode, false);
                await LoadStudents();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                // NotificationService.ShowError($"Không thể reset: {ex.Message}");
            }
        }
        private async Task ConfirmReset(string studentCode)
        {
            bool confirmed = await JS.InvokeAsync<bool>("confirm", new object[] { $"Bạn có chắc chắn muốn reset đăng nhập cho sinh viên {studentCode}?" });
            if (confirmed)
            {
                await ResetLogin(studentCode);
            }
        }

    }
}
