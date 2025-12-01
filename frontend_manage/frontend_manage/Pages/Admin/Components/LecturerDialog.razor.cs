using frontend_manage.DTOs;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.Admin;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class LecturerDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public LecturerDto? Lecturer { get; set; }
    [Parameter] public List<DepartmentDto> Departments { get; set; } = new();
    
    [Inject] private LecturerService LecturerService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    
    private LecturerCreateDto lecturer = new();
    private bool isEdit = false;

    protected override void OnInitialized()
    {
        if (Lecturer != null)
        {
            isEdit = true;
            lecturer.LecturerCode = Lecturer.LecturerCode;
            lecturer.FirstName = Lecturer.FirstName;
            lecturer.LastName = Lecturer.LastName;
            lecturer.Email = Lecturer.Email;
            lecturer.PhoneNumber = Lecturer.PhoneNumber;
            lecturer.DepartmentId = Lecturer.DepartmentId;
            lecturer.DateOfBirth = Lecturer.DateOfBirth;
            lecturer.Gender = Lecturer.Gender;
        }
    }

    private async Task HandleSubmit()
    {
        try
        {
            if (isEdit)
            {
                var updateDto = new LecturerUpdateDto
                {
                    FirstName = lecturer.FirstName,
                    LastName = lecturer.LastName,
                    Email = lecturer.Email,
                    PhoneNumber = lecturer.PhoneNumber,
                    DepartmentId = lecturer.DepartmentId,
                    DateOfBirth = lecturer.DateOfBirth,
                    Gender = lecturer.Gender
                };
                var success = await LecturerService.UpdateLecturerAsync(lecturer.LecturerCode, updateDto);
                if (success)
                {
                    Snackbar.Add("Cập nhật giảng viên thành công", Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add("Lỗi khi cập nhật giảng viên", Severity.Error);
                }
            }
            else
            {
                var result = await LecturerService.AddLecturerAsync(lecturer);
                if (result != null)
                {
                    Snackbar.Add("Thêm giảng viên thành công", Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add("Lỗi khi thêm giảng viên", Severity.Error);
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }

    private void Cancel() => MudDialog.Cancel();
}


