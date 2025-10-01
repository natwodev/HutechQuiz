using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs.AcademicAffairs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MudBlazor;
using frontend_manage.Pages.AcademicAffairs.Components.Dialogs;
using frontend_manage.Services.AcademicAffairs;
using System;
using System.Threading;
using System.Globalization;

namespace frontend_manage.Pages.AcademicAffairs.Components
{
    public partial class ExamBatchesTab : ComponentBase
    {
        [Parameter] public List<ExamBatchDto> ExamBatches { get; set; } = new();
        [Parameter] public EventCallback OnRefresh { get; set; }
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private ExamBatchService ExamBatchService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        private ExamBatchDto? _editingExamBatch;
        private ExamBatchDto? _deletingExamBatch;
        
        // Form state
        private bool _showForm = false;
        private bool _isEditMode = false;
        private ExamBatchDto _formData = new();
        private string _startDateString = "";
        private string _endDateString = "";
        private int _totalItems;
        private string _searchText = string.Empty;
        private string _statusFilter = "all";
        private MudTable<ExamBatchDto>? _tableRef;
        private int _page;
        private int _pageSize = 10;
        private List<ExamBatchDto> _currentPageItems = new();

        private void CreateNew()
        {
            _formData = new ExamBatchDto
            {
                IsActive = false
            };
            _isEditMode = false;
            _showForm = true;
            InitializeDateTimeFields();
            StateHasChanged();
        }

        private void EditExamBatch(ExamBatchDto examBatch)
        {
            _formData = new ExamBatchDto
            {
                ExamBatchId = examBatch.ExamBatchId,
                BatchName = examBatch.BatchName,
                Description = examBatch.Description,
                StartDate = examBatch.StartDate,
                EndDate = examBatch.EndDate,
                IsActive = examBatch.IsActive,
                SemesterId = examBatch.SemesterId
            };
            _isEditMode = true;
            _showForm = true;
            InitializeDateTimeFields();
            StateHasChanged();
        }

        private void InitializeDateTimeFields()
        {
            if (_formData.StartDate != DateTime.MinValue && _formData.StartDate.Year > 1900)
            {
                _startDateString = _formData.StartDate.ToString("yyyy-MM-dd");
            }
            else
            {
                _startDateString = DateTime.Today.ToString("yyyy-MM-dd");
                // Ensure model has a valid default date if user doesn't touch input
                _formData.StartDate = DateTime.Today.Date;
            }

            if (_formData.EndDate != DateTime.MinValue && _formData.EndDate.Year > 1900)
            {
                _endDateString = _formData.EndDate.ToString("yyyy-MM-dd");
            }
            else
            {
                _endDateString = DateTime.Today.ToString("yyyy-MM-dd");
                // Ensure model has a valid default date if user doesn't touch input
                _formData.EndDate = DateTime.Today.Date;
            }
        }

        private void OnStartDateChanged(ChangeEventArgs e)
        {
            _startDateString = e.Value?.ToString() ?? "";
            if (DateTime.TryParseExact(_startDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
            {
                _formData.StartDate = startDate.Date;
            }
        }

        private void OnEndDateChanged(ChangeEventArgs e)
        {
            _endDateString = e.Value?.ToString() ?? "";
            if (DateTime.TryParseExact(_endDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
            {
                _formData.EndDate = endDate.Date;
            }
        }

        private void CancelForm()
        {
            _showForm = false;
            _formData = new ExamBatchDto();
        }

        private async Task SaveForm()
        {
            try
            {
                // Sync from strings in case user didn't change date inputs after open
                if ((_formData.StartDate == DateTime.MinValue || _formData.StartDate.Year <= 1900) &&
                    DateTime.TryParseExact(_startDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startParsed))
                {
                    _formData.StartDate = startParsed.Date;
                }
                if ((_formData.EndDate == DateTime.MinValue || _formData.EndDate.Year <= 1900) &&
                    DateTime.TryParseExact(_endDateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endParsed))
                {
                    _formData.EndDate = endParsed.Date;
                }

                // Validate BatchName
                if (string.IsNullOrWhiteSpace(_formData.BatchName))
                {
                    Snackbar.Add("Vui lòng nhập tên đợt thi", Severity.Error);
                    return;
                }

                // Validate dates
                if (_formData.StartDate == DateTime.MinValue || _formData.EndDate == DateTime.MinValue)
                {
                    Snackbar.Add("Vui lòng nhập ngày bắt đầu và kết thúc", Severity.Error);
                    return;
                }

                // Validate EndDate >= StartDate by date (allow same day)
                if (_formData.EndDate.Date < _formData.StartDate.Date)
                {
                    Snackbar.Add("Ngày kết thúc không được trước ngày bắt đầu", Severity.Error);
                    return;
                }

                if (_isEditMode)
                {
                    await ExamBatchService.UpdateAsync(_formData.ExamBatchId, new ExamBatchUpdateDto
                    {
                        BatchName = _formData.BatchName,
                        Description = _formData.Description,
                        StartDate = _formData.StartDate,
                        EndDate = _formData.EndDate,
                        IsActive = _formData.IsActive,
                        SemesterId = _formData.SemesterId
                    });
                    Snackbar.Add("Cập nhật đợt thi thành công", Severity.Success);
                }
                else
                {
                    await ExamBatchService.CreateAsync(new ExamBatchCreateDto
                    {
                        BatchName = _formData.BatchName,
                        Description = _formData.Description,
                        StartDate = _formData.StartDate,
                        EndDate = _formData.EndDate,
                        IsActive = _formData.IsActive,
                        SemesterId = 1 // TODO: Get from context
                    });
                    Snackbar.Add("Tạo đợt thi thành công", Severity.Success);
                }

                _showForm = false;
                _formData = new ExamBatchDto();
                
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

        private async Task<TableData<ExamBatchDto>> LoadExamBatches(TableState state, CancellationToken cancellationToken)
        {
            try
            {
                var page = _page + 1;
                var pageSize = _pageSize;
                var paged = await ExamBatchService.GetPagedAsync(page, pageSize);
                // Client-side filter/search for now (backend can be extended later)
                IEnumerable<ExamBatchDto> items = paged.Items;
                if (!string.IsNullOrWhiteSpace(_searchText))
                {
                    var term = _searchText.Trim().ToLowerInvariant();
                    items = items.Where(x => (x.BatchName ?? string.Empty).ToLowerInvariant().Contains(term)
                                           || (x.Description ?? string.Empty).ToLowerInvariant().Contains(term));
                }
                if (_statusFilter == "active") items = items.Where(x => x.IsActive);
                else if (_statusFilter == "inactive") items = items.Where(x => !x.IsActive);
                var filtered = items.ToList();
                _totalItems = paged.TotalItems;
                _currentPageItems = filtered;
                return new TableData<ExamBatchDto>
                {
                    Items = filtered,
                    TotalItems = string.IsNullOrWhiteSpace(_searchText) && _statusFilter == "all" ? paged.TotalItems : filtered.Count
                };
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Tải danh sách đợt thi thất bại: {ex.Message}", Severity.Error);
                return new TableData<ExamBatchDto> { Items = new List<ExamBatchDto>(), TotalItems = 0 };
            }
        }

        private async Task TableReload()
        {
            if (_tableRef != null)
            {
                await _tableRef.ReloadServerData();
            }
        }

        private async Task OnSearchInput(ChangeEventArgs e)
        {
            _searchText = e.Value?.ToString() ?? string.Empty;
            await TableReload();
        }

        private async Task OnStatusChanged(ChangeEventArgs e)
        {
            _statusFilter = e.Value?.ToString() ?? "all";
            await TableReload();
        }

        private int LastPageIndex()
        {
            if (_pageSize <= 0) return 0;
            var pages = (int)Math.Ceiling((_totalItems <= 0 ? 1 : _totalItems) / (double)_pageSize);
            return Math.Max(0, pages - 1);
        }

        private async Task GoPrev()
        {
            _page = Math.Max(0, _page - 1);
            await TableReload();
        }

        private async Task GoNext()
        {
            _page = Math.Min(LastPageIndex(), _page + 1);
            await TableReload();
        }

        private async Task OnPageSizeChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var sz) && sz > 0)
            {
                _pageSize = sz;
                _page = 0; // reset to first page
                await TableReload();
            }
        }

        private async Task DeleteExamBatch(ExamBatchDto examBatch)
        {
            _deletingExamBatch = examBatch;
            
        var parameters = new DialogParameters<DeleteConfirmDialog>
        {
            { nameof(DeleteConfirmDialog.Title), "Xác nhận xóa đợt thi" },
            { nameof(DeleteConfirmDialog.Content), $"Bạn có chắc chắn muốn xóa đợt thi '{examBatch.BatchName}'? Hành động này không thể hoàn tác." },
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
                    await ExamBatchService.DeleteAsync(examBatch.ExamBatchId);
                    Snackbar.Add($"Đã xóa đợt thi '{examBatch.BatchName}'", Severity.Success);
                    // Optimistic update: remove from current page immediately
                    _currentPageItems = _currentPageItems.Where(x => x.ExamBatchId != examBatch.ExamBatchId).ToList();
                    StateHasChanged();
                    await TableReload();
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

    }
}
