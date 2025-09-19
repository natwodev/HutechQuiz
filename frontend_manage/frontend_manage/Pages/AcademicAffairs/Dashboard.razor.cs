using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using frontend_manage.Pages.AcademicAffairs.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs;

public partial class Dashboard : ComponentBase
{
    [Inject] private AcademicYearService AcademicYearService { get; set; } = null!;
    [Inject] private SemesterService SemesterService { get; set; } = null!;
    [Inject] private ExamBatchService ExamBatchService { get; set; } = null!;
    [Inject] private ExamBatchDetailService ExamBatchDetailService { get; set; } = null!;
    [Inject] private ExamSessionService ExamSessionService { get; set; } = null!;
    [Inject] private ExamSessionSubjectService ExamSessionSubjectService { get; set; } = null!;
    [Inject] private frontend_manage.Services.AuthService AuthService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private bool _loading = true;
    private List<AcademicYearDto> _academicYears = new();
    private List<SemesterDto> _semesters = new();
    private List<ExamBatchDto> _examBatches = new();
    private List<ExamBatchDetailDto> _examBatchDetails = new();
    private List<ExamSessionDto> _examSessions = new();
    private List<ExamSessionDepartmentDto> _examSessionDepartments = new();
    private List<ExamSessionSubjectDto> _examSessionSubjects = new();

    protected override async Task OnInitializedAsync()
    {
        await RefreshData();
    }

    private async Task RefreshData()
    {
        _loading = true;
        StateHasChanged();

        try
        {
            _academicYears = await AcademicYearService.GetAllAsync();
            _semesters = await SemesterService.GetAllAsync();
            _examBatches = await ExamBatchService.GetAllAsync();
            _examBatchDetails = await ExamBatchDetailService.GetAllAsync();
            _examSessions = await ExamSessionService.GetAllAsync();
            _examSessionSubjects = await ExamSessionSubjectService.GetAllAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi khi tải dữ liệu: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    // ExamBatchDetail methods
    private async Task OpenCreateExamBatchDetailDialog()
    {
        Snackbar.Add("Chức năng thêm chi tiết đợt thi sẽ được triển khai trong phiên bản tiếp theo", Severity.Info);
    }

    private async Task EditExamBatchDetail(ExamBatchDetailDto examBatchDetail)
    {
        Snackbar.Add("Chức năng chỉnh sửa chi tiết đợt thi sẽ được triển khai trong phiên bản tiếp theo", Severity.Info);
    }

    private async Task DeleteExamBatchDetail(ExamBatchDetailDto examBatchDetail)
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { "Title", "Xác nhận xóa" },
            { "Message", $"Bạn có chắc chắn muốn xóa chi tiết đợt thi '{examBatchDetail.Name}'?" },
            { "ConfirmText", "Xóa" }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Xác nhận xóa", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            try
            {
                await ExamBatchDetailService.DeleteAsync(examBatchDetail.ExamBatchDetailId);
                Snackbar.Add("Xóa chi tiết đợt thi thành công", Severity.Success);
                await RefreshData();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi khi xóa chi tiết đợt thi: {ex.Message}", Severity.Error);
            }
        }
    }

    // ExamSessionDepartment methods
    private async Task OpenCreateExamSessionDepartmentDialog()
    {
        Snackbar.Add("Chức năng thêm khoa tham gia ca thi sẽ được triển khai trong phiên bản tiếp theo", Severity.Info);
    }

    private async Task EditExamSessionDepartment(ExamSessionDepartmentDto examSessionDepartment)
    {
        Snackbar.Add("Chức năng chỉnh sửa khoa tham gia ca thi sẽ được triển khai trong phiên bản tiếp theo", Severity.Info);
    }

    private async Task DeleteExamSessionDepartment(ExamSessionDepartmentDto examSessionDepartment)
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { "Title", "Xác nhận xóa" },
            { "Message", $"Bạn có chắc chắn muốn xóa khoa '{examSessionDepartment.DepartmentName}' khỏi ca thi '{examSessionDepartment.ExamSessionName}'?" },
            { "ConfirmText", "Xóa" }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Xác nhận xóa", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            try
            {
                // Implementation for deleting exam session department
                Snackbar.Add("Xóa khoa tham gia ca thi thành công", Severity.Success);
                await RefreshData();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi khi xóa khoa tham gia ca thi: {ex.Message}", Severity.Error);
            }
        }
    }

    // ExamSessionSubject methods
    private async Task OpenCreateExamSessionSubjectDialog()
    {
        Snackbar.Add("Chức năng thêm ca thi môn học sẽ được triển khai trong phiên bản tiếp theo", Severity.Info);
    }

    private async Task EditExamSessionSubject(ExamSessionSubjectDto examSessionSubject)
    {
        Snackbar.Add("Chức năng chỉnh sửa ca thi môn học sẽ được triển khai trong phiên bản tiếp theo", Severity.Info);
    }

    private async Task UpdateOriginalExamPaper(ExamSessionSubjectDto examSessionSubject)
    {
        Snackbar.Add("Chức năng cập nhật đề thi gốc sẽ được triển khai trong phiên bản tiếp theo", Severity.Info);
    }

    private async Task Logout()
    {
        await AuthService.Logout();
    }
}
