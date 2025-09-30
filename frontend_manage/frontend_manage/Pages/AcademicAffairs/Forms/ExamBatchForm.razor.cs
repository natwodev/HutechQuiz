using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using MudBlazor;
using System;
using System.Threading.Tasks;

namespace frontend_manage.Pages.AcademicAffairs.Forms
{
    public partial class ExamBatchForm
    {
        [Parameter] public int Id { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private ExamBatchService ExamBatchService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        private ExamBatchDto ExamBatch = new();
        private bool IsEdit => Id != 0;
        private bool isLoading = false;

        private DateTime? _startDate;
        private DateTime? _endDate;

        protected override async Task OnInitializedAsync()
        {
            if (IsEdit)
            {
                // Load existing exam batch
                try
                {
                    var examBatch = await ExamBatchService.GetByIdAsync(Id);
                    if (examBatch != null)
                    {
                        ExamBatch = examBatch;
                        InitializeDateTimeFields();
                    }
                    else
                    {
                        Snackbar.Add("Không tìm thấy đợt thi", Severity.Error);
                        Navigation.NavigateTo("/academic-affairs/dashboard/1");
                    }
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Lỗi khi tải đợt thi: {ex.Message}", Severity.Error);
                    Navigation.NavigateTo("/academic-affairs/dashboard/1");
                }
            }
            else
            {
                // Initialize for new exam batch
                InitializeDateTimeFields();
            }
        }

        private void InitializeDateTimeFields()
        {
            if (ExamBatch.StartDate != DateTime.MinValue && ExamBatch.StartDate.Year > 1900)
            {
                _startDate = ExamBatch.StartDate.Date;
            }
            else
            {
                _startDate = DateTime.Today;
                ExamBatch.StartDate = DateTime.Today;
            }

            if (ExamBatch.EndDate != DateTime.MinValue && ExamBatch.EndDate.Year > 1900)
            {
                _endDate = ExamBatch.EndDate.Date;
            }
            else
            {
                _endDate = DateTime.Today;
                ExamBatch.EndDate = DateTime.Today;
            }
        }

        private DateTime? StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                if (value.HasValue)
                {
                    ExamBatch.StartDate = value.Value.Date;
                }
            }
        }

        private DateTime? EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                if (value.HasValue)
                {
                    ExamBatch.EndDate = value.Value.Date;
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
                    // Update existing exam batch
                    await ExamBatchService.UpdateAsync(Id, new ExamBatchUpdateDto
                    {
                        BatchName = ExamBatch.BatchName,
                        Description = ExamBatch.Description,
                        StartDate = ExamBatch.StartDate,
                        EndDate = ExamBatch.EndDate,
                        IsActive = ExamBatch.IsActive,
                        SemesterId = ExamBatch.SemesterId
                    });
                    Snackbar.Add("Cập nhật đợt thi thành công", Severity.Success);
                }
                else
                {
                    // Create new exam batch
                    await ExamBatchService.CreateAsync(new ExamBatchCreateDto
                    {
                        BatchName = ExamBatch.BatchName,
                        Description = ExamBatch.Description,
                        StartDate = ExamBatch.StartDate,
                        EndDate = ExamBatch.EndDate,
                        IsActive = ExamBatch.IsActive,
                        SemesterId = ExamBatch.SemesterId
                    });
                    Snackbar.Add("Tạo đợt thi thành công", Severity.Success);
                }

                Navigation.NavigateTo("/academic-affairs/dashboard/1");
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
            Navigation.NavigateTo("/academic-affairs/dashboard/1");
        }
    }
}
