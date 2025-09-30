using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using MudBlazor;
using System;
using System.Threading.Tasks;

namespace frontend_manage.Pages.AcademicAffairs.Forms
{
    public partial class AcademicYearForm
    {
        [Parameter] public int Id { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private AcademicYearService AcademicYearService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        private AcademicYearDto AcademicYear = new();
        private bool IsEdit => Id != 0;
        private bool isLoading = false;

        protected override async Task OnInitializedAsync()
        {
            if (IsEdit)
            {
                // Load existing academic year
                try
                {
                    var academicYear = await AcademicYearService.GetByIdAsync(Id);
                    if (academicYear != null)
                    {
                        AcademicYear = academicYear;
                    }
                    else
                    {
                        Snackbar.Add("Không tìm thấy năm học", Severity.Error);
                        Navigation.NavigateTo("/academic-affairs/dashboard/3");
                    }
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Lỗi khi tải năm học: {ex.Message}", Severity.Error);
                    Navigation.NavigateTo("/academic-affairs/dashboard/3");
                }
            }
        }

        private async Task Save()
        {
            try
            {
                isLoading = true;
                StateHasChanged();

                if (IsEdit)
                {
                    // Update existing academic year
                    await AcademicYearService.UpdateAsync(Id, new AcademicYearUpdateDto
                    {
                        YearName = AcademicYear.YearName
                    });
                    Snackbar.Add("Cập nhật năm học thành công", Severity.Success);
                }
                else
                {
                    // Create new academic year
                    await AcademicYearService.CreateAsync(new AcademicYearCreateDto
                    {
                        YearName = AcademicYear.YearName
                    });
                    Snackbar.Add("Tạo năm học thành công", Severity.Success);
                }

                Navigation.NavigateTo("/academic-affairs/dashboard/3");
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private void Cancel()
        {
            Navigation.NavigateTo("/academic-affairs/dashboard/3");
        }
    }
}
