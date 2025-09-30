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
                IsActive = examBatch.IsActive
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
            }

            if (_formData.EndDate != DateTime.MinValue && _formData.EndDate.Year > 1900)
            {
                _endDateString = _formData.EndDate.ToString("yyyy-MM-dd");
            }
            else
            {
                _endDateString = DateTime.Today.ToString("yyyy-MM-dd");
            }
        }

        private void OnStartDateChanged(ChangeEventArgs e)
        {
            _startDateString = e.Value?.ToString() ?? "";
            if (DateTime.TryParse(_startDateString, out var startDate))
            {
                _formData.StartDate = startDate.Date;
            }
        }

        private void OnEndDateChanged(ChangeEventArgs e)
        {
            _endDateString = e.Value?.ToString() ?? "";
            if (DateTime.TryParse(_endDateString, out var endDate))
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
                        IsActive = _formData.IsActive
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
                // TODO: Call API to delete exam batch
                if (OnRefresh.HasDelegate)
                {
                    await OnRefresh.InvokeAsync();
                }
            }
        }

    }
}
