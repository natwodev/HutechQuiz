using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs.AcademicAffairs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MudBlazor;
using frontend_manage.Pages.AcademicAffairs.Components.Dialogs;
using frontend_manage.Services.AcademicAffairs;
using System;

namespace frontend_manage.Pages.AcademicAffairs.Components
{
    public partial class AcademicYearsTab : ComponentBase
    {
        [Parameter] public List<AcademicYearDto> AcademicYears { get; set; } = new();
        [Parameter] public EventCallback OnRefresh { get; set; }
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private AcademicYearService AcademicYearService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        private AcademicYearDto? _editingAcademicYear;
        private AcademicYearDto? _deletingAcademicYear;
        
        // Form state
        private bool _showForm = false;
        private bool _isEditMode = false;
        private AcademicYearDto _formData = new();
        private string _searchText = string.Empty;

        private IEnumerable<AcademicYearDto> FilteredAcademicYears => string.IsNullOrWhiteSpace(_searchText)
            ? AcademicYears
            : AcademicYears.Where(x => (x.YearName ?? string.Empty).ToLowerInvariant().Contains(_searchText.Trim().ToLowerInvariant()));

        private int _page;
        private int _pageSize = 10;
        private IEnumerable<AcademicYearDto> PagedAcademicYears => FilteredAcademicYears
            .Skip(_page * _pageSize)
            .Take(_pageSize);

        private void CreateNew()
        {
            _formData = new AcademicYearDto();
            _isEditMode = false;
            _showForm = true;
            StateHasChanged();
        }

        private void EditAcademicYear(AcademicYearDto academicYear)
        {
            _formData = new AcademicYearDto
            {
                AcademicYearId = academicYear.AcademicYearId,
                YearName = academicYear.YearName
            };
            _isEditMode = true;
            _showForm = true;
            StateHasChanged();
        }

        private void CancelForm()
        {
            _showForm = false;
            _formData = new AcademicYearDto();
        }

        private async Task SaveForm()
        {
            try
            {
                // Validate YearName
                if (string.IsNullOrWhiteSpace(_formData.YearName))
                {
                    Snackbar.Add("Vui lòng nhập tên năm học", Severity.Error);
                    return;
                }

                if (_isEditMode)
                {
                    await AcademicYearService.UpdateAsync(_formData.AcademicYearId, new AcademicYearUpdateDto
                    {
                        YearName = _formData.YearName
                    });
                    Snackbar.Add("Cập nhật năm học thành công", Severity.Success);
                }
                else
                {
                    await AcademicYearService.CreateAsync(new AcademicYearCreateDto
                    {
                        YearName = _formData.YearName
                    });
                    Snackbar.Add("Tạo năm học thành công", Severity.Success);
                }

                _showForm = false;
                _formData = new AcademicYearDto();
                
                if (OnRefresh.HasDelegate)
                {
                    await OnRefresh.InvokeAsync();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Lỗi: {ex.Message}", Severity.Error);
            }
        }

        private async Task DeleteAcademicYear(AcademicYearDto academicYear)
        {
            _deletingAcademicYear = academicYear;
            
        var parameters = new DialogParameters<DeleteConfirmDialog>
        {
            { nameof(DeleteConfirmDialog.Title), "Xác nhận xóa năm học" },
            { nameof(DeleteConfirmDialog.Content), $"Bạn có chắc chắn muốn xóa năm học '{academicYear.YearName}'? Hành động này không thể hoàn tác." },
            { nameof(DeleteConfirmDialog.ConfirmText), "Xóa" },
            { nameof(DeleteConfirmDialog.CancelText), "Hủy" }
        };

            var options = new DialogOptions()
            {
                MaxWidth = MaxWidth.Small,
                CloseButton = true,
            };

            var dialog = await DialogService.ShowAsync<DeleteConfirmDialog>("Xác nhận xóa", parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                try
                {
                    await AcademicYearService.DeleteAsync(academicYear.AcademicYearId);
                    Snackbar.Add($"Đã xóa năm học '{academicYear.YearName}'", Severity.Success);
                    if (OnRefresh.HasDelegate)
                    {
                        await OnRefresh.InvokeAsync();
                    }
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Xóa thất bại: {ex.Message}", Severity.Error);
                }
            }
        }

        private void OnSearchInput(ChangeEventArgs e)
        {
            _searchText = e.Value?.ToString() ?? string.Empty;
            StateHasChanged();
        }

        private int LastPageIndex()
        {
            var total = FilteredAcademicYears.Count();
            if (_pageSize <= 0) return 0;
            var pages = (int)Math.Ceiling((total <= 0 ? 1 : total) / (double)_pageSize);
            return Math.Max(0, pages - 1);
        }

        private void GoPrev()
        {
            _page = Math.Max(0, _page - 1);
        }

        private void GoNext()
        {
            _page = Math.Min(LastPageIndex(), _page + 1);
        }

        private void OnPageSizeChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var sz) && sz > 0)
            {
                _pageSize = sz;
                _page = 0;
            }
        }

    }
}
