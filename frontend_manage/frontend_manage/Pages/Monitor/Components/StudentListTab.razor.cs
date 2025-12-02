using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using frontend_manage.DTOs;
using frontend_manage.Services;
using frontend_manage.Enums;
using OfficeOpenXml;
using System.IO;

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
        
        [Inject]
        private IJSRuntime JSRuntime { get; set; }

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

        private async Task ToggleIsLoginForAllStudentsAsync(bool isLogin)
        {
            if (!ExamSessionSubjectId.HasValue)
            {
                Snackbar.Add("Không tìm thấy ExamSessionSubjectId", Severity.Error);
                return;
            }

            // Hiển thị dialog xác nhận
            var confirmMessage = isLogin 
                ? $"Bạn có chắc chắn muốn KHÓA đăng nhập cho TẤT CẢ sinh viên trong ca thi này? ({TotalStudents} sinh viên)"
                : $"Bạn có chắc chắn muốn MỞ đăng nhập cho TẤT CẢ sinh viên trong ca thi này? ({TotalStudents} sinh viên)";

            var parameters = new DialogParameters
            {
                { "ContentText", confirmMessage },
                { "ButtonText", isLogin ? "Khóa đăng nhập" : "Mở đăng nhập" },
                { "Color", isLogin ? Color.Error : Color.Success }
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                Position = DialogPosition.Center
            };

            var dialog = await DialogService.ShowAsync<frontend_manage.Pages.Admin.Components.ConfirmDialog>("Xác nhận", parameters, options);
            var result = await dialog.Result;

            if (result.Canceled)
            {
                return;
            }

            try
            {
                Snackbar.Add("Đang xử lý...", Severity.Info);
                var (success, message, updatedCount) = await MonitorService.ToggleIsLoginForAllStudentsAsync(
                    ExamSessionSubjectId.Value, 
                    isLogin);

                if (success)
                {
                    Snackbar.Add(message, Severity.Success);
                    
                    // Cập nhật trạng thái IsLogin cho tất cả sinh viên trong danh sách
                    if (Students != null)
                    {
                        foreach (var student in Students)
                        {
                            student.IsLogin = isLogin;
                        }
                        StateHasChanged();
                    }
                }
                else
                {
                    Snackbar.Add(message, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
            }
        }

        private void MessageStudent(string studentCode)
        {
            Snackbar.Add("Tính năng đang phát triển", Severity.Info);
        }

        private async Task ToggleStudentLogin(StudentExamRoomStatusDto student)
        {
            if (student == null)
            {
                Snackbar.Add("Không tìm thấy sinh viên!", Severity.Error);
                return;
            }

            // Đảo trạng thái đăng nhập
            bool newLoginStatus = !student.IsLogin;
            var result = await MonitorService.ActiveLoginAsync(student.StudentCode, newLoginStatus);
            Snackbar.Add(result, Severity.Success);

            // Cập nhật trạng thái trong danh sách
            student.IsLogin = newLoginStatus;
            StateHasChanged();
        }

        private void ViewStudentDetails(StudentExamRoomStatusDto student)
        {
            if (student == null)
            {
                Snackbar.Add("Không tìm thấy sinh viên!", Severity.Error);
                return;
            }

            var message = $"Mã SV: {student.StudentCode}\n" +
                         $"Họ tên: {student.FirstName} {student.LastName}\n" +
                         $"Trạng thái đăng nhập: {(student.IsLogin ? "Đã đăng nhập" : "Chưa đăng nhập")}\n" +
                         $"Trạng thái thi: {GetExamStatusDisplayName(GetExamStatus(student))}\n" +
                         $"Thời gian cộng: {student.ExtraMinutes} phút\n" +
                         $"Điểm số: {student.Score.ToString("0.0")}";

            Snackbar.Add(message, Severity.Info);
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

        private async Task ShowCheatingWarnings(StudentExamRoomStatusDto student)
        {
            if (student == null || student.CheatingWarningCount <= 0)
                return;

            string content;

            if (student.CheatingWarningDetails != null && student.CheatingWarningDetails.Any())
            {
                var lines = student.CheatingWarningDetails
                    .Select((w, idx) => $"{idx + 1}. {w}")
                    .ToList();
                content = string.Join("\n", lines);
            }
            else
            {
                content =
                    $"Đã ghi nhận {student.CheatingWarningCount} cảnh báo gian lận cho sinh viên {student.StudentCode}.\n" +
                    "Hiện hệ thống chỉ lưu số lần cảnh báo, chưa có chi tiết từng lần.";
            }

            await DialogService.ShowMessageBox(
                $"Cảnh báo gian lận - {student.StudentCode}",
                content,
                yesText: "Đóng");
        }

        protected override void OnParametersSet()
        {
            // Khi parent truyền Students mới → ép re-render lại UI
            SeedFakeCheatingWarnings(); // Dữ liệu ảo demo UI cảnh báo gian lận
            StateHasChanged();
        }

        /// <summary>
        /// TẠM THỜI: sinh dữ liệu ảo cho cột cảnh báo gian lận để demo UI.
        /// Khi backend có dữ liệu thật thì xoá/hủy hàm này.
        /// </summary>
        private void SeedFakeCheatingWarnings()
        {
            if (Students == null || !Students.Any())
                return;

            // Nếu đã có dữ liệu thật (được map từ backend) thì không đụng vào
            if (Students.Any(s => s.CheatingWarningCount > 0 || 
                                  (s.CheatingWarningDetails != null && s.CheatingWarningDetails.Any())))
                return;

            var random = new Random();

            foreach (var student in Students)
            {
                // Xác suất nhỏ để tránh quá nhiều cảnh báo ảo
                var roll = random.Next(0, 100);
                if (roll < 15) // 15% sinh viên có cảnh báo
                {
                    var count = random.Next(1, 4); // 1–3 cảnh báo
                    student.CheatingWarningCount = count;
                    student.CheatingWarningDetails ??= new();
                    student.CheatingWarningDetails.Clear();

                    for (int i = 0; i < count; i++)
                    {
                        var reasonIndex = random.Next(0, 3);
                        string reason = reasonIndex switch
                        {
                            0 => "Rời khỏi tab thi (trình duyệt bị ẩn / chuyển tab).",
                            1 => "Thoát chế độ toàn màn hình trong khi đang làm bài.",
                            2 => "Chuyển sang ứng dụng khác trong lúc thi.",
                            _ => "Hệ thống ghi nhận hành vi bất thường khi làm bài."
                        };

                        student.CheatingWarningDetails.Add(reason);
                    }
                }
            }
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

        private async Task ExportStudentGradesToExcelAsync()
        {
            if (Students == null || !Students.Any())
            {
                Snackbar.Add("Không có dữ liệu sinh viên để export", Severity.Warning);
                return;
            }

            try
            {
                Snackbar.Add("Đang tạo file Excel...", Severity.Info);

                // Tạo danh sách StudentGradeDto từ dữ liệu có sẵn
                var grades = Students.Select((student, index) => new StudentGradeDto
                {
                    STT = index + 1,
                    StudentCode = student.StudentCode,
                    Score = student.Score // Score đã có giá trị mặc định là 0
                }).ToList();

                // Tạo file Excel
                var excelBytes = await CreateExcelFileAsync(grades);

                if (excelBytes != null && excelBytes.Length > 0)
                {
                    // Tạo tên file với timestamp
                    string fileName = $"BangDiem_ESS{ExamSessionSubjectId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    
                    // Gọi JavaScript để download file
                    await JSRuntime.InvokeVoidAsync("downloadExcelFile", fileName, Convert.ToBase64String(excelBytes));
                    
                    Snackbar.Add("Tải bảng điểm thành công!", Severity.Success);
                }
                else
                {
                    Snackbar.Add("Không thể tạo file Excel. Vui lòng thử lại sau.", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting grades: {ex.Message}");
                Snackbar.Add($"Lỗi khi tạo file Excel: {ex.Message}", Severity.Error);
            }
        }

        private async Task<byte[]> CreateExcelFileAsync(List<StudentGradeDto> grades)
        {
            try
            {
                // Set EPPlus license context
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Bảng Điểm");
                
                // Tạo header
                worksheet.Cells[1, 1].Value = "STT";
                worksheet.Cells[1, 2].Value = "Mã Sinh Viên";
                worksheet.Cells[1, 3].Value = "Điểm";
                
                // Style header
                var headerRange = worksheet.Cells[1, 1, 1, 3];
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                
                // Fill data
                int row = 2;
                foreach (var grade in grades)
                {
                    worksheet.Cells[row, 1].Value = grade.STT;
                    worksheet.Cells[row, 2].Value = grade.StudentCode;
                    worksheet.Cells[row, 3].Value = grade.Score;
                    row++;
                }
                
                // Auto fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                
                var excelBytes = package.GetAsByteArray();
                return excelBytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Excel file: {ex.Message}");
                throw;
            }
        }

        // DTO class cho bảng điểm
        public class StudentGradeDto
        {
            public int STT { get; set; }
            public string StudentCode { get; set; } = string.Empty;
            public double Score { get; set; }
        }
    }
}
