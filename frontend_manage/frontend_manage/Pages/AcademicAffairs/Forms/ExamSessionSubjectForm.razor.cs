using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;
using MudBlazor;
using System;
using System.Threading.Tasks;

namespace frontend_manage.Pages.AcademicAffairs.Forms
{
    public partial class ExamSessionSubjectForm
    {
        [Parameter] public int Id { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private ExamSessionSubjectService ExamSessionSubjectService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        private ExamSessionSubjectDto ExamSessionSubject = new();
        private bool IsEdit => Id != 0;
        private bool isLoading = false;

        private DateTime? _startDate;
        private TimeSpan? _startTime;

        protected override async Task OnInitializedAsync()
        {
            if (IsEdit)
            {
                // Load existing exam session subject
                try
                {
                    var examSessionSubject = await ExamSessionSubjectService.GetByIdAsync(Id);
                    if (examSessionSubject != null)
                    {
                        ExamSessionSubject = examSessionSubject;
                        InitializeDateTimeFields();
                    }
                    else
                    {
                        Snackbar.Add("Không tìm thấy môn thi", Severity.Error);
                        Navigation.NavigateTo("/academic-affairs/dashboard/2");
                    }
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Lỗi khi tải môn thi: {ex.Message}", Severity.Error);
                    Navigation.NavigateTo("/academic-affairs/dashboard/2");
                }
            }
            else
            {
                // Initialize for new exam session subject
                InitializeDateTimeFields();
            }
        }

        private void InitializeDateTimeFields()
        {
            if (ExamSessionSubject.StartTime != DateTime.MinValue && ExamSessionSubject.StartTime.Year > 1900)
            {
                _startDate = ExamSessionSubject.StartTime.Date;
                _startTime = ExamSessionSubject.StartTime.TimeOfDay;
            }
            else
            {
                _startDate = DateTime.Today;
                _startTime = TimeSpan.Zero;
                ExamSessionSubject.StartTime = DateTime.Today;
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

        private TimeSpan? StartTimeSpan
        {
            get => _startTime;
            set
            {
                _startTime = value;
                UpdateStartTime();
            }
        }

        private void UpdateStartTime()
        {
            if (_startDate.HasValue && _startTime.HasValue)
            {
                ExamSessionSubject.StartTime = _startDate.Value.Date.Add(_startTime.Value);
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
                    // Update existing exam session subject
                    await ExamSessionSubjectService.UpdateAsync(Id, new ExamSessionSubjectUpdateDto
                    {
                        Duration = ExamSessionSubject.Duration,
                        StartTime = ExamSessionSubject.StartTime,
                        IsCompleted = ExamSessionSubject.IsCompleted,
                        ExamSessionId = ExamSessionSubject.ExamSessionId,
                        SubjectId = ExamSessionSubject.SubjectId,
                        OriginalExamPaperId = ExamSessionSubject.OriginalExamPaperId,
                        EndTime = ExamSessionSubject.EndTime,
                        ExamSessionSubjectCore = ExamSessionSubject.ExamSessionSubjectCore,
                        ExamRoomId = ExamSessionSubject.ExamRoomId,
                        MonitorId = ExamSessionSubject.MonitorId
                    });
                    Snackbar.Add("Cập nhật môn thi thành công", Severity.Success);
                }
                else
                {
                    // Create new exam session subject
                    await ExamSessionSubjectService.CreateAsync(new ExamSessionSubjectCreateDto
                    {
                        Duration = ExamSessionSubject.Duration,
                        StartTime = ExamSessionSubject.StartTime,
                        IsCompleted = ExamSessionSubject.IsCompleted,
                        ExamSessionId = ExamSessionSubject.ExamSessionId,
                        SubjectId = ExamSessionSubject.SubjectId,
                        OriginalExamPaperId = ExamSessionSubject.OriginalExamPaperId,
                        EndTime = ExamSessionSubject.EndTime,
                        ExamSessionSubjectCore = ExamSessionSubject.ExamSessionSubjectCore,
                        ExamRoomId = ExamSessionSubject.ExamRoomId,
                        MonitorId = ExamSessionSubject.MonitorId
                    });
                    Snackbar.Add("Tạo môn thi thành công", Severity.Success);
                }

                Navigation.NavigateTo("/academic-affairs/dashboard/2");
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
            Navigation.NavigateTo("/academic-affairs/dashboard/2");
        }
    }
}
