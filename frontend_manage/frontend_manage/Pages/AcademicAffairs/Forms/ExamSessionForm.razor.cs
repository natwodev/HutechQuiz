using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using MudBlazor;
using System;
using System.Threading.Tasks;

namespace frontend_manage.Pages.AcademicAffairs.Forms
{
    public partial class ExamSessionForm
    {
        [Parameter] public int Id { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private ExamSessionService ExamSessionService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        private ExamSessionDto ExamSession = new();
        private bool IsEdit => Id != 0;
        private bool isLoading = false;

        private DateTime? _startDate;
        private DateTime? _endDate;
        private TimeSpan? _startTime;
        private TimeSpan? _endTime;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                if (IsEdit)
                {
                    // Load existing exam session
                    Console.WriteLine($"Loading exam session with ID: {Id}");
                    var examSession = await ExamSessionService.GetByIdAsync(Id);
                    if (examSession != null)
                    {
                        ExamSession = examSession;
                        InitializeDateTimeFields();
                        Console.WriteLine($"Successfully loaded exam session: {examSession.Name}");
                    }
                    else
                    {
                        Snackbar.Add("Không tìm thấy ca thi", Severity.Error);
                        Navigation.NavigateTo("/academic-affairs/dashboard/0");
                    }
                }
                else
                {
                    // Initialize for new exam session
                    InitializeDateTimeFields();
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Error loading exam session: {httpEx.Message}");
                Snackbar.Add($"Lỗi kết nối: {httpEx.Message}", Severity.Error);
                Navigation.NavigateTo("/academic-affairs/dashboard/0");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading exam session: {ex.Message}");
                Snackbar.Add($"Lỗi khi tải ca thi: {ex.Message}", Severity.Error);
                Navigation.NavigateTo("/academic-affairs/dashboard/0");
            }
        }

        private void InitializeDateTimeFields()
        {
            if (ExamSession.StartTime != DateTime.MinValue && ExamSession.StartTime.Year > 1900)
            {
                _startDate = ExamSession.StartTime.Date;
                _startTime = ExamSession.StartTime.TimeOfDay;
            }
            else
            {
                _startDate = DateTime.Today;
                _startTime = TimeSpan.Zero;
                ExamSession.StartTime = DateTime.Today;
            }

            if (ExamSession.EndTime != DateTime.MinValue && ExamSession.EndTime.Year > 1900)
            {
                _endDate = ExamSession.EndTime.Date;
                _endTime = ExamSession.EndTime.TimeOfDay;
            }
            else
            {
                _endDate = DateTime.Today;
                _endTime = TimeSpan.Zero;
                ExamSession.EndTime = DateTime.Today;
            }
        }

        private DateTime? StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                UpdateStartTime();
            }
        }

        private DateTime? EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                UpdateEndTime();
            }
        }

        private TimeSpan? StartTimeSpan
        {
            get => _startTime;
            set
            {
                _startTime = value;
                UpdateStartTime();
            }
        }

        private TimeSpan? EndTimeSpan
        {
            get => _endTime;
            set
            {
                _endTime = value;
                UpdateEndTime();
            }
        }

        private void UpdateStartTime()
        {
            if (_startDate.HasValue && _startTime.HasValue)
            {
                ExamSession.StartTime = _startDate.Value.Date.Add(_startTime.Value);
            }
        }

        private void UpdateEndTime()
        {
            if (_endDate.HasValue && _endTime.HasValue)
            {
                ExamSession.EndTime = _endDate.Value.Date.Add(_endTime.Value);
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
                    // Update existing exam session
                    await ExamSessionService.UpdateAsync(Id, new ExamSessionUpdateDto
                    {
                        Name = ExamSession.Name,
                        StartTime = ExamSession.StartTime,
                        EndTime = ExamSession.EndTime,
                        IsActive = ExamSession.IsActive,
                        IsCompleted = ExamSession.IsCompleted,
                        ExamBatchDetailId = ExamSession.ExamBatchDetailId
                    });
                    Snackbar.Add("Cập nhật ca thi thành công", Severity.Success);
                }
                else
                {
                    // Create new exam session
                    await ExamSessionService.CreateAsync(new ExamSessionCreateDto
                    {
                        Name = ExamSession.Name,
                        StartTime = ExamSession.StartTime,
                        EndTime = ExamSession.EndTime,
                        IsActive = ExamSession.IsActive,
                        IsCompleted = ExamSession.IsCompleted,
                        ExamBatchDetailId = ExamSession.ExamBatchDetailId
                    });
                    Snackbar.Add("Tạo ca thi thành công", Severity.Success);
                }

                Navigation.NavigateTo("/academic-affairs/dashboard");
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
            Navigation.NavigateTo("/academic-affairs/dashboard");
        }
    }
}

