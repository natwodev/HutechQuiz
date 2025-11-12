using frontend_manage.DTOs.AcademicAffairs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.Admin.Components;

public partial class DepartmentTab : ComponentBase
{
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] private frontend_manage.Services.AuthService AuthService { get; set; } = default!;

    [Parameter]
    public List<DepartmentDto> Departments { get; set; } = new();
    
    [Parameter]
    public EventCallback OnDepartmentUpdated { get; set; }
    
    private string searchString = "";

    private bool FilterFunc(DepartmentDto department)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;
        
        return department.DepartmentName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
               department.DepartmentId.Contains(searchString, StringComparison.OrdinalIgnoreCase);
    }

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters { ["Department"] = new DepartmentCreateDto() };
        var dialog = await DialogService.ShowAsync<DepartmentDialog>("Thêm khoa mới", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnDepartmentUpdated.InvokeAsync();
        }
    }

    private async Task OpenEditDialog(DepartmentDto department)
    {
        var parameters = new DialogParameters { ["Department"] = department };
        var dialog = await DialogService.ShowAsync<DepartmentDialog>("Sửa khoa", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled && result.Data != null)
        {
            await OnDepartmentUpdated.InvokeAsync();
        }
    }

    private async Task OpenDeleteDialog(DepartmentDto department)
    {
        var parameters = new DialogParameters 
        { 
            ["Message"] = $"Bạn có chắc chắn muốn xóa khoa '{department.DepartmentName}'?" 
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Xác nhận xóa", parameters);
        var result = await dialog.Result;
        
        if (!result.Canceled)
        {
            await DeleteDepartment(department);
        }
    }

    private async Task DeleteDepartment(DepartmentDto department)
    {
        try
        {
            var httpClient = HttpClientFactory.CreateClient("API");
            var token = await AuthService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            
            var response = await httpClient.DeleteAsync($"/api/Department/{department.DepartmentId}");
            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add("Xóa khoa thành công", Severity.Success);
                await OnDepartmentUpdated.InvokeAsync();
            }
            else
            {
                Snackbar.Add("Lỗi khi xóa khoa", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
        }
    }
}


