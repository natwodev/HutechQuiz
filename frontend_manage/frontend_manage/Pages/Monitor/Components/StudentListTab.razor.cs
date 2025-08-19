using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using frontend_manage.DTOs;
using frontend_manage.Services;
using frontend_manage.Enums;

namespace frontend_manage.Pages.Monitor.Components
{
    public partial class StudentListTab : ComponentBase
    {
        [Inject]
        private ISnackbar Snackbar { get; set; }
        
        [Inject]
        private IDialogService DialogService { get; set; }
       
        [Inject]
        private MonitorService MonitorService { get; set; }

        [Parameter]
        public List<StudentExamRoomStatusDto>? Students { get; set; }

        [Parameter]
        public int? ExamSessionSubjectId { get; set; }

        [Parameter]
        public string? ExamSessionName { get; set; }

        [Parameter]
        public DateTime? ExamSessionStartTime { get; set; }

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
            _ = ManageStudentAsync(studentCode);
        }

        private async Task ManageStudentAsync(string studentCode)
        {
            // Tìm sinh viên theo mã
            var student = Students?.FirstOrDefault(s => s.StudentCode == studentCode);
            if (student == null)
            {
                Snackbar.Add("Không tìm thấy sinh viên!", Severity.Error);
                return;
            }

            // Đảo trạng thái đăng nhập (ví dụ: nếu đang đăng nhập thì chuyển thành chưa đăng nhập)
            bool newLoginStatus = !student.IsLogin;
            var result = await MonitorService.ActiveLoginAsync(studentCode, newLoginStatus);
            Snackbar.Add(result, Severity.Success);

            // Cập nhật trạng thái trong danh sách (nếu muốn cập nhật UI ngay)
            student.IsLogin = newLoginStatus;
            StateHasChanged();
        }
        
        
        
        private async Task AddExtraTime(StudentExamRoomStatusDto student)
        {
            if (student == null) return;
            
            var parameters = new DialogParameters
            {
                { "StudentCode", student.StudentCode },
                { "StudentExamSessionId", student.StudentExamSessionId },
                { "TotalExamMinutes", student.Duration }
            };
            
            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                Position = DialogPosition.Center
            };
            
            var dialog = await DialogService.ShowAsync<AddExtraMinutesDialog>("Thêm thời gian thi", parameters, options);
            var result = await dialog.Result;
            
            if (!result.Canceled)
            {
                // Xử lý kết quả từ dialog
                var extraMinutesObj = (result.Data as dynamic);
                if (extraMinutesObj != null)
                {
                    int extraMinutes = extraMinutesObj.ExtraMinutes;
                    string reason = extraMinutesObj.Reason;
                    
                    // Hiển thị thông báo
                    Snackbar.Add($"Đã thêm {extraMinutes} phút cho sinh viên {student.StudentCode}. Lý do: {reason}", Severity.Success);
                    
                    // TODO: Gọi API để cập nhật thời gian thi
                    // await ExamManagementService.AddExtraMinutesAsync(student.StudentCode, student.StudentExamSessionId, extraMinutes, reason);
                    
                    // Refresh data
                    await RefreshStudentData();
                }
            }
        }
        protected override void OnParametersSet()
        {
            // Khi parent truyền Students mới → ép re-render lại UI
            StateHasChanged();
        }

        private async Task RefreshStudentData()
        {
            // Notify parent component to refresh data
            Snackbar.Add("Đang làm mới dữ liệu...", Severity.Info);
            StateHasChanged();
        }
        
        private ExamStatus GetExamStatus(StudentExamRoomStatusDto student)
        {
            // Nếu đã hoàn thành thi
            if (student.IsCompleted)
            {
                return ExamStatus.Completed;
            }

            // Nếu StartTime == null (chưa bắt đầu thi)
            if (student.StartTime == null)
            {
                // Kiểm tra xem có quá 15 phút kể từ ExamSessionStartTime không
                if (ExamSessionStartTime.HasValue)
                {
                    var timeDifference = DateTime.Now - ExamSessionStartTime.Value;
                    if (timeDifference.TotalMinutes <= 15)
                    {
                        return ExamStatus.NotStarted; // Chưa vào thi
                    }
                    else
                    {
                        return ExamStatus.Dropped; // Bỏ thi (quá 15 phút)
                    }
                }
                else
                {
                    // Nếu không có ExamSessionStartTime, mặc định là chưa vào thi
                    return ExamStatus.NotStarted;
                }
            }

            // Trường hợp còn lại: đang thi
            return ExamStatus.InProgress;
        }

  
        private string GetExamStatusDisplayName(ExamStatus status)
        {
            return status switch
            {
                ExamStatus.NotStarted => "Chưa vào thi",
                ExamStatus.InProgress => "Đang thi",
                ExamStatus.Dropped => "Bỏ thi",
                ExamStatus.Completed => "Đã hoàn thành",
                _ => "Không xác định"
            };
        }
        
        private string GetExamStatusCssClass(ExamStatus status)
        {
            return status switch
            {
                ExamStatus.NotStarted => "status-tag warning",
                ExamStatus.InProgress => "status-tag info",
                ExamStatus.Dropped => "status-tag danger",
                ExamStatus.Completed => "status-tag success",
                _ => "status-tag default"
            };
        }
    }
}
