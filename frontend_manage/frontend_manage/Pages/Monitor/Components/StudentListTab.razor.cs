using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using frontend_manage.DTOs;

namespace frontend_manage.Pages.Monitor.Components
{
    public partial class StudentListTab : ComponentBase
    {
        [Inject]
        private ISnackbar Snackbar { get; set; }

        [Parameter]
        public List<StudentExamRoomStatusDto>? Students { get; set; }

        [Parameter]
        public int? ExamSessionSubjectId { get; set; }

        private string searchText = "";
        private int currentPage = 1;
        private int pageSize = 10;

        private int TotalStudents => Students?.Count ?? 0;
        private int LoggedInStudents => Students?.Count(s => s.IsLogin) ?? 0;
        private int TakingExamStudents => Students?.Count(s => !s.IsCompleted) ?? 0;
        private int SubmittedStudents => Students?.Count(s => s.IsCompleted) ?? 0;

        private List<StudentExamRoomStatusDto> FilteredStudents
        {
            get
            {
                if (Students == null) return new List<StudentExamRoomStatusDto>();
                
                return string.IsNullOrWhiteSpace(searchText)
                    ? Students
                    : Students.Where(s =>
                        s.StudentCode.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        s.FirstName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        s.LastName.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }

        private void PerformSearch()
        {
            currentPage = 1; // Reset to first page when searching
            StateHasChanged();
        }

        private void RefreshData()
        {
            // Refresh data from parent component
            StateHasChanged();
            Snackbar.Add("Dữ liệu đã được làm mới", Severity.Success);
        }

        private void MessageStudent(string studentCode)
        {
            Snackbar.Add($"Gửi thông báo cho sinh viên: {studentCode}", Severity.Info);
        }

        private void ManageStudent(string studentCode)
        {
            Snackbar.Add($"Quản lý sinh viên: {studentCode}", Severity.Info);
        }
    }
}
