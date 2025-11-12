using frontend_manage.DTOs;
using frontend_manage.Services.Admin;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class StudentManagementTab : ComponentBase
{
    [Parameter]
    public List<StudentInfoDto> Students { get; set; } = new();
    
    [Parameter]
    public EventCallback OnStudentUpdated { get; set; }
    
    [Inject] private AdminStudentService AdminStudentService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    
    private string searchString = "";

    private bool FilterFunc(StudentInfoDto student)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;
        
        return student.StudentCode.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
               student.FirstName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
               student.LastName.Contains(searchString, StringComparison.OrdinalIgnoreCase);
    }

    private async Task ToggleLoginStatus(StudentInfoDto student)
    {
        try
        {
            var newStatus = !(student.IsLogin == true);
            var success = await AdminStudentService.ActiveLoginAsync(student.StudentCode, newStatus);
            if (success)
            {
                Snackbar.Add($"Đã {(newStatus ? "kích hoạt" : "vô hiệu hóa")} đăng nhập cho sinh viên {student.StudentCode}", Severity.Success);
                await OnStudentUpdated.InvokeAsync();
            }
            else
            {
                Snackbar.Add("Lỗi khi cập nhật trạng thái đăng nhập", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }

    private async Task ViewStudentDetails(StudentInfoDto student)
    {
        var parameters = new DialogParameters { ["Student"] = student };
        var dialog = await DialogService.ShowAsync<StudentDetailsDialog>("Chi tiết sinh viên", parameters);
    }

    private async Task OpenImportDialog()
    {
        var parameters = new DialogParameters();
        var dialog = await DialogService.ShowAsync<StudentImportDialog>("Import sinh viên từ Excel", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnStudentUpdated.InvokeAsync();
        }
    }
}


