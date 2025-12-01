using frontend_manage.DTOs.AcademicAffairs;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Net.Http.Json;

namespace frontend_manage.Pages.Admin.Components;

public partial class DepartmentDialog : ComponentBase
{
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public DepartmentDto? Department { get; set; }
    
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private frontend_manage.Services.AuthService AuthService { get; set; } = default!;
    
    private DepartmentCreateDto department = new();
    private bool isEdit = false;

    protected override void OnInitialized()
    {
        if (Department != null)
        {
            isEdit = true;
            department.DepartmentId = Department.DepartmentId;
            department.DepartmentName = Department.DepartmentName;
        }
    }

    private async Task HandleSubmit()
    {
        try
        {
            var httpClient = HttpClientFactory.CreateClient("API");
            var token = await AuthService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            if (isEdit)
            {
                var updateDto = new DepartmentUpdateDto { DepartmentName = department.DepartmentName };
                var response = await httpClient.PutAsJsonAsync($"/api/Department/{department.DepartmentId}", updateDto);
                if (response.IsSuccessStatusCode)
                {
                    Snackbar.Add("Cập nhật khoa thành công", Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add("Lỗi khi cập nhật khoa", Severity.Error);
                }
            }
            else
            {
                var response = await httpClient.PostAsJsonAsync("/api/Department", department);
                if (response.IsSuccessStatusCode)
                {
                    Snackbar.Add("Thêm khoa thành công", Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add("Lỗi khi thêm khoa", Severity.Error);
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


