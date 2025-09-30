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
    public partial class ExamSessionSubjectsTab : ComponentBase
    {
        [Parameter] public List<ExamSessionSubjectDto> ExamSessionSubjects { get; set; } = new();
        [Parameter] public EventCallback OnRefresh { get; set; }
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private ExamSessionSubjectService ExamSessionSubjectService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        private ExamSessionSubjectDto? _editingExamSessionSubject;
        private ExamSessionSubjectDto? _deletingExamSessionSubject;
        
        // Form state
        private bool _showForm = false;
        private bool _isEditMode = false;
        private ExamSessionSubjectDto _formData = new();
        private string _examDateString = "";
        private string _examTimeString = "";

        private void CreateNew()
        {
            _formData = new ExamSessionSubjectDto
            {
                IsCompleted = false
            };
            _isEditMode = false;
            _showForm = true;
            InitializeDateTimeFields();
            StateHasChanged();
        }

        private void EditExamSessionSubject(ExamSessionSubjectDto examSessionSubject)
        {
            _formData = new ExamSessionSubjectDto
            {
                ExamSessionSubjectId = examSessionSubject.ExamSessionSubjectId,
                SubjectName = examSessionSubject.SubjectName,
                RoomName = examSessionSubject.RoomName,
                MonitorName = examSessionSubject.MonitorName,
                StartTime = examSessionSubject.StartTime,
                IsCompleted = examSessionSubject.IsCompleted
            };
            _isEditMode = true;
            _showForm = true;
            InitializeDateTimeFields();
            StateHasChanged();
        }

        private void InitializeDateTimeFields()
        {
            if (_formData.StartTime != DateTime.MinValue && _formData.StartTime.Year > 1900)
            {
                _examDateString = _formData.StartTime.ToString("yyyy-MM-dd");
                _examTimeString = _formData.StartTime.ToString("HH:mm");
            }
            else
            {
                _examDateString = DateTime.Today.ToString("yyyy-MM-dd");
                _examTimeString = "00:00";
            }
        }

        private void OnExamDateChanged(ChangeEventArgs e)
        {
            _examDateString = e.Value?.ToString() ?? "";
            UpdateExamDate();
        }

        private void OnExamTimeChanged(ChangeEventArgs e)
        {
            _examTimeString = e.Value?.ToString() ?? "";
            UpdateExamDate();
        }

        private void UpdateExamDate()
        {
            if (DateTime.TryParse(_examDateString, out var examDate) && TimeSpan.TryParse(_examTimeString, out var examTime))
            {
                _formData.StartTime = examDate.Date + examTime;
            }
            else if (DateTime.TryParse(_examDateString, out var dateOnly))
            {
                _formData.StartTime = dateOnly.Date;
            }
            else
            {
                _formData.StartTime = DateTime.MinValue;
            }
        }

        private void CancelForm()
        {
            _showForm = false;
            _formData = new ExamSessionSubjectDto();
        }

        private async Task SaveForm()
        {
            try
            {
                // Validate Duration
                if (_formData.Duration <= 0)
                {
                    Snackbar.Add("Vui lòng nhập thời gian thi hợp lệ", Severity.Error);
                    return;
                }

                // Validate StartTime
                if (_formData.StartTime == DateTime.MinValue)
                {
                    Snackbar.Add("Vui lòng nhập thời gian bắt đầu", Severity.Error);
                    return;
                }

                if (_isEditMode)
                {
                    await ExamSessionSubjectService.UpdateAsync(_formData.ExamSessionSubjectId, new ExamSessionSubjectUpdateDto
                    {
                        ExamSessionId = _formData.ExamSessionId,
                        SubjectId = _formData.SubjectId,
                        Duration = _formData.Duration,
                        OriginalExamPaperId = _formData.OriginalExamPaperId,
                        IsCompleted = _formData.IsCompleted,
                        StartTime = _formData.StartTime,
                        EndTime = _formData.EndTime,
                        ExamSessionSubjectCore = _formData.ExamSessionSubjectCore,
                        ExamRoomId = _formData.ExamRoomId,
                        MonitorId = _formData.MonitorId
                    });
                    Snackbar.Add("Cập nhật môn thi thành công", Severity.Success);
                }
                else
                {
                    await ExamSessionSubjectService.CreateAsync(new ExamSessionSubjectCreateDto
                    {
                        ExamSessionId = 1, // TODO: Get from context
                        SubjectId = 1, // TODO: Get from context
                        Duration = 60, // TODO: Get from form
                        OriginalExamPaperId = null,
                        IsCompleted = _formData.IsCompleted,
                        StartTime = _formData.StartTime,
                        EndTime = null,
                        ExamSessionSubjectCore = "",
                        ExamRoomId = null,
                        MonitorId = null
                    });
                    Snackbar.Add("Tạo môn thi thành công", Severity.Success);
                }

                _showForm = false;
                _formData = new ExamSessionSubjectDto();
                
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

        private async Task DeleteExamSessionSubject(ExamSessionSubjectDto examSessionSubject)
        {
            _deletingExamSessionSubject = examSessionSubject;
            
        var parameters = new DialogParameters<DeleteConfirmDialog>
        {
            { nameof(DeleteConfirmDialog.Title), "Xác nhận xóa môn thi" },
            { nameof(DeleteConfirmDialog.Content), $"Bạn có chắc chắn muốn xóa môn thi '{examSessionSubject.SubjectName}'? Hành động này không thể hoàn tác." },
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
                // TODO: Call API to delete exam session subject
                if (OnRefresh.HasDelegate)
                {
                    await OnRefresh.InvokeAsync();
                }
            }
        }

    }
}
