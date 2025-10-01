using Microsoft.AspNetCore.Components;
using MudBlazor;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Pages.AcademicAffairs.Components.Dialogs;
using frontend_manage.Services.AcademicAffairs;
using System;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;

namespace frontend_manage.Pages.AcademicAffairs.Components
{
    public partial class ExamSessionsTab : ComponentBase
    {
        [Parameter] public List<ExamSessionDto> ExamSessions { get; set; } = new();
        [Parameter] public EventCallback OnRefresh { get; set; }
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private ExamSessionService ExamSessionService { get; set; } = default!;
        [Inject] private ExamBatchDetailService ExamBatchDetailService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        private ExamSessionDto? _editingExamSession;
        private ExamSessionDto? _deletingExamSession;
        
        // Form state
        private bool _showForm = false;
        private bool _isEditMode = false;
        private ExamSessionDto _formData = new();
        private string _startDateString = "";
        private string _endDateString = "";
        private string _startTimeString = "";
        private string _endTimeString = "";
        private int _startHour;
        private int _startMinute;
        private int _endHour;
        private int _endMinute;
        private List<ExamBatchDetailDto> _examBatchDetails = new();
        private int _totalItems;
        private string _searchText = string.Empty;
        private string _statusFilter = "all";
        private MudTable<ExamSessionDto>? _tableRef;
        private int _page;
        private int _pageSize = 10;
        private List<ExamSessionDto> _currentPageItems = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadExamBatchDetails();
        }

        private async Task LoadExamBatchDetails()
        {
            try
            {
                _examBatchDetails = await ExamBatchDetailService.GetAllAsync();
                Console.WriteLine($"Loaded {_examBatchDetails.Count} exam batch details");
                foreach (var detail in _examBatchDetails)
                {
                    Console.WriteLine($"- {detail.Name} (ID: {detail.ExamBatchDetailId})");
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading exam batch details: {ex.Message}");
                Snackbar.Add($"Lỗi khi tải danh sách đợt thi: {ex.Message}", Severity.Error);
                _examBatchDetails = new List<ExamBatchDetailDto>();
                StateHasChanged();
            }
        }

        private void CreateNew()
        {
            _formData = new ExamSessionDto
            {
                ExamBatchDetailId = 0, // Khởi tạo giá trị mặc định
                IsCompleted = false
            };
            _isEditMode = false;
            _showForm = true;
            InitializeDateTimeFields();
            StateHasChanged(); // Force UI update
        }

        private void EditExamSession(ExamSessionDto examSession)
        {
            _formData = new ExamSessionDto
            {
                ExamSessionId = examSession.ExamSessionId,
                Name = examSession.Name,
                StartTime = examSession.StartTime,
                EndTime = examSession.EndTime,
                IsActive = examSession.IsActive,
                ExamBatchDetailId = examSession.ExamBatchDetailId,
                IsCompleted = examSession.IsCompleted
            };
            _isEditMode = true;
            _showForm = true;
            InitializeDateTimeFields();
            StateHasChanged(); // Force UI update
        }

        private void InitializeDateTimeFields()
        {
            try
            {
                if (_formData.StartTime != DateTime.MinValue && _formData.StartTime.Year > 1900)
                {
                    _startDateString = _formData.StartTime.ToString("yyyy-MM-dd");
                    _startTimeString = _formData.StartTime.ToString("HH:mm");
                    _startHour = _formData.StartTime.Hour;
                    _startMinute = _formData.StartTime.Minute;
                }
                else
                {
                    var vnNow = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime;
                    _startDateString = vnNow.ToString("yyyy-MM-dd");
                    _startTimeString = vnNow.ToString("HH:mm");
                    _formData.StartTime = new DateTime(vnNow.Year, vnNow.Month, vnNow.Day, vnNow.Hour, vnNow.Minute, 0);
                    _startHour = vnNow.Hour;
                    _startMinute = vnNow.Minute;
                }

                if (_formData.EndTime != DateTime.MinValue && _formData.EndTime.Year > 1900)
                {
                    _endDateString = _formData.EndTime.ToString("yyyy-MM-dd");
                    _endTimeString = _formData.EndTime.ToString("HH:mm");
                    _endHour = _formData.EndTime.Hour;
                    _endMinute = _formData.EndTime.Minute;
                }
                else
                {
                    var baseStart = _formData.StartTime != DateTime.MinValue ? _formData.StartTime : DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime;
                    var end = baseStart.AddHours(1);
                    _endDateString = end.ToString("yyyy-MM-dd");
                    _endTimeString = end.ToString("HH:mm");
                    _formData.EndTime = new DateTime(end.Year, end.Month, end.Day, end.Hour, end.Minute, 0);
                    _endHour = end.Hour;
                    _endMinute = end.Minute;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing datetime fields: {ex.Message}");
                var vnNow = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime;
                _startDateString = vnNow.ToString("yyyy-MM-dd");
                _startTimeString = vnNow.ToString("HH:mm");
                var end = vnNow.AddHours(1);
                _endDateString = end.ToString("yyyy-MM-dd");
                _endTimeString = end.ToString("HH:mm");
                _formData.StartTime = new DateTime(vnNow.Year, vnNow.Month, vnNow.Day, vnNow.Hour, vnNow.Minute, 0);
                _formData.EndTime = new DateTime(end.Year, end.Month, end.Day, end.Hour, end.Minute, 0);
                _startHour = vnNow.Hour;
                _startMinute = vnNow.Minute;
                _endHour = end.Hour;
                _endMinute = end.Minute;
            }
        }

        private void OnStartDateChanged(ChangeEventArgs e)
        {
            _startDateString = e.Value?.ToString() ?? "";
            UpdateExamSessionStartTime();
            StateHasChanged();
        }

        private void OnStartTimeChanged(ChangeEventArgs e)
        {
            _startTimeString = e.Value?.ToString() ?? "";
            UpdateExamSessionStartTime();
            StateHasChanged();
        }

        private void UpdateExamSessionStartTime()
        {
            try
            {
                var hasDate = DateTime.TryParseExact(
                    _startDateString,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var startDateExact);

                var current = (_formData.StartTime != DateTime.MinValue && _formData.StartTime.Year > 1900)
                    ? _formData.StartTime
                    : DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime;

                var finalDate = hasDate ? startDateExact.Date : current.Date;

                // Prefer numeric hour/minute selections to avoid string parse bugs
                var hour = Math.Clamp(_startHour, 0, 23);
                var minute = Math.Clamp(_startMinute, 0, 59);
                var finalTime = new TimeSpan(hour, minute, 0);

                // Keep the string in sync for any inputs bound to it
                _startTimeString = $"{hour:00}:{minute:00}";

                _formData.StartTime = finalDate + finalTime;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error computing start time: {ex.Message}");
                // Preserve current if valid; otherwise set to VN now
                if (!(_formData.StartTime != DateTime.MinValue && _formData.StartTime.Year > 1900))
                {
                    var vnNow = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime;
                    _formData.StartTime = new DateTime(vnNow.Year, vnNow.Month, vnNow.Day, vnNow.Hour, vnNow.Minute, 0);
                }
            }
        }

        private void OnEndDateChanged(ChangeEventArgs e)
        {
            _endDateString = e.Value?.ToString() ?? "";
            UpdateExamSessionEndTime();
            StateHasChanged();
        }

        private void OnEndTimeChanged(ChangeEventArgs e)
        {
            _endTimeString = e.Value?.ToString() ?? "";
            UpdateExamSessionEndTime();
            StateHasChanged();
        }

        private void UpdateExamSessionEndTime()
        {
            try
            {
                var hasDate = DateTime.TryParseExact(
                    _endDateString,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var endDateExact);

                var current = (_formData.EndTime != DateTime.MinValue && _formData.EndTime.Year > 1900)
                    ? _formData.EndTime
                    : ((_formData.StartTime != DateTime.MinValue && _formData.StartTime.Year > 1900)
                        ? _formData.StartTime.AddHours(1)
                        : DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime.AddHours(1));

                var finalDate = hasDate ? endDateExact.Date : current.Date;

                // Prefer numeric hour/minute selections to avoid string parse bugs
                var hour = Math.Clamp(_endHour, 0, 23);
                var minute = Math.Clamp(_endMinute, 0, 59);
                var finalTime = new TimeSpan(hour, minute, 0);

                // Keep the string in sync for any inputs bound to it
                _endTimeString = $"{hour:00}:{minute:00}";

                _formData.EndTime = finalDate + finalTime;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error computing end time: {ex.Message}");
                // Preserve current if valid; otherwise set to VN now + 1h
                if (!(_formData.EndTime != DateTime.MinValue && _formData.EndTime.Year > 1900))
                {
                    var vnNow = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime.AddHours(1);
                    _formData.EndTime = new DateTime(vnNow.Year, vnNow.Month, vnNow.Day, vnNow.Hour, vnNow.Minute, 0);
                }
            }
        }



        private void CancelForm()
        {
            _showForm = false;
            _formData = new ExamSessionDto();
            _isEditMode = false;
            StateHasChanged();
        }

        private async Task SaveForm()
        {
            try
            {
                // Sync date/time from input strings in case user didn't change fields
                UpdateExamSessionStartTime();
                UpdateExamSessionEndTime();

                // Validate Name
                if (string.IsNullOrWhiteSpace(_formData.Name))
                {
                    Snackbar.Add("Vui lòng nhập tên ca thi", Severity.Error);
                    return;
                }

                // Validate ExamBatchDetailId
                if (_formData.ExamBatchDetailId == 0)
                {
                    Snackbar.Add("Vui lòng chọn đợt thi", Severity.Error);
                    return;
                }

                // Validate date/time
                if (_formData.StartTime == DateTime.MinValue || _formData.EndTime == DateTime.MinValue)
                {
                    Snackbar.Add("Vui lòng nhập ngày giờ hợp lệ", Severity.Error);
                    return;
                }

                // Validate EndTime > StartTime
                if (_formData.EndTime <= _formData.StartTime)
                {
                    Snackbar.Add("Thời gian kết thúc phải lớn hơn thời gian bắt đầu", Severity.Error);
                    return;
                }

                if (_isEditMode)
                {
                    await ExamSessionService.UpdateAsync(_formData.ExamSessionId, new ExamSessionUpdateDto
                    {
                        Name = _formData.Name,
                        StartTime = _formData.StartTime,
                        EndTime = _formData.EndTime,
                        IsActive = _formData.IsActive,
                        IsCompleted = _formData.IsCompleted,
                        ExamBatchDetailId = _formData.ExamBatchDetailId
                    });
                    Snackbar.Add("Cập nhật ca thi thành công", Severity.Success);
                }
                else
                {
                    await ExamSessionService.CreateAsync(new ExamSessionCreateDto
                    {
                        Name = _formData.Name,
                        StartTime = _formData.StartTime,
                        EndTime = _formData.EndTime,
                        IsActive = _formData.IsActive,
                        IsCompleted = false, // Mới tạo nên chưa hoàn thành
                        ExamBatchDetailId = _formData.ExamBatchDetailId
                    });
                    Snackbar.Add("Tạo ca thi thành công", Severity.Success);
                }

                _showForm = false;
                _formData = new ExamSessionDto();
                StateHasChanged();
                
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

        private async Task<TableData<ExamSessionDto>> LoadExamSessions(TableState state, CancellationToken cancellationToken)
        {
            try
            {
                var page = _page + 1;
                var pageSize = _pageSize;
                var paged = await ExamSessionService.GetPagedAsync(page, pageSize);
                IEnumerable<ExamSessionDto> items = paged.Items;
                if (!string.IsNullOrWhiteSpace(_searchText))
                {
                    var term = _searchText.Trim().ToLowerInvariant();
                    items = items.Where(x => (x.Name ?? string.Empty).ToLowerInvariant().Contains(term));
                }
                items = _statusFilter switch
                {
                    "active" => items.Where(x => x.IsActive && !x.IsCompleted),
                    "inactive" => items.Where(x => !x.IsActive && !x.IsCompleted),
                    "completed" => items.Where(x => x.IsCompleted),
                    _ => items
                };
                var filtered = items.ToList();
                _totalItems = paged.TotalItems;
                _currentPageItems = filtered;
                return new TableData<ExamSessionDto>
                {
                    Items = filtered,
                    TotalItems = string.IsNullOrWhiteSpace(_searchText) && _statusFilter == "all" ? paged.TotalItems : filtered.Count
                };
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Tải danh sách ca thi thất bại: {ex.Message}", Severity.Error);
                return new TableData<ExamSessionDto> { Items = new List<ExamSessionDto>(), TotalItems = 0 };
            }
        }

        private async Task TableReload()
        {
            if (_tableRef != null)
            {
                await _tableRef.ReloadServerData();
            }
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
                _page = 0;
                await TableReload();
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

        private async Task DeleteExamSession(ExamSessionDto examSession)
        {
            _deletingExamSession = examSession;
            
            var parameters = new DialogParameters<DeleteConfirmDialog>
            {
                { nameof(DeleteConfirmDialog.Title), "Xác nhận xóa ca thi" },
                { nameof(DeleteConfirmDialog.Content), $"Bạn có chắc chắn muốn xóa ca thi '{examSession.Name}'? Hành động này không thể hoàn tác." },
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
                    await ExamSessionService.DeleteAsync(examSession.ExamSessionId);
                    Snackbar.Add($"Đã xóa ca thi '{examSession.Name}'", Severity.Success);
                    // Reload bảng ngay để biến mất tức thì
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

        private void OnStartHourChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var hour))
            {
                _startHour = Math.Clamp(hour, 0, 23);
                _startTimeString = $"{_startHour:00}:{_startMinute:00}";
                UpdateExamSessionStartTime();
                StateHasChanged();
            }
        }

        private void OnStartMinuteChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var minute))
            {
                _startMinute = Math.Clamp(minute, 0, 59);
                _startTimeString = $"{_startHour:00}:{_startMinute:00}";
                UpdateExamSessionStartTime();
                StateHasChanged();
            }
        }

        private void AfterStartMinuteChanged()
        {
            _startMinute = Math.Clamp(_startMinute, 0, 59);
            _startTimeString = $"{_startHour:00}:{_startMinute:00}";
            UpdateExamSessionStartTime();
        }

        private void OnEndHourChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var hour))
            {
                _endHour = Math.Clamp(hour, 0, 23);
                _endTimeString = $"{_endHour:00}:{_endMinute:00}";
                UpdateExamSessionEndTime();
                StateHasChanged();
            }
        }

        private void AfterEndHourChanged()
        {
            _endHour = Math.Clamp(_endHour, 0, 23);
            _endTimeString = $"{_endHour:00}:{_endMinute:00}";
            UpdateExamSessionEndTime();
        }

        private void OnEndMinuteChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var minute))
            {
                _endMinute = Math.Clamp(minute, 0, 59);
                _endTimeString = $"{_endHour:00}:{_endMinute:00}";
                UpdateExamSessionEndTime();
                StateHasChanged();
            }
        }

        private void AfterEndMinuteChanged()
        {
            _endMinute = Math.Clamp(_endMinute, 0, 59);
            _endTimeString = $"{_endHour:00}:{_endMinute:00}";
            UpdateExamSessionEndTime();
        }

        private void AfterStartHourChanged()
        {
            _startHour = Math.Clamp(_startHour, 0, 23);
            _startTimeString = $"{_startHour:00}:{_startMinute:00}";
            UpdateExamSessionStartTime();
        }

    }
}
