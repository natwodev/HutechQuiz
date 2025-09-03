using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Timers;

namespace frontend_manage.Pages.Exam.Components
{
    // Class để lưu trữ thông tin timer
    public class ExamTimerData
    {
        public int StudentExamSessionId { get; set; }
        public DateTime StartTime { get; set; }
        public double Duration { get; set; } // Thời gian còn lại tính bằng phút
    }

    public partial class ExamTimer : BaseComponent
    {
        [Parameter] public string FormattedTime { get; set; } = "00:00";
        [Parameter] public int RemainingMinutes { get; set; }
        [Parameter] public int ExtraMinutes { get; set; }
        [Parameter] public bool IsTimeUp { get; set; }
        [Parameter] public bool IsSubmitDisabled { get; set; }
        [Parameter] public EventCallback OnSubmitExam { get; set; }
        
        [Parameter] public int StudentExamSessionId { get; set; }
        [Parameter] public int TotalDuration { get; set; }
        [Parameter] public EventCallback<ExamTimerData> OnTimerInitialized { get; set; }
        [Parameter] public EventCallback OnTimeUp { get; set; }
        
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        
        private const string TIMER_STORAGE_KEY = "exam_timer_data";
        private System.Timers.Timer? examTimer;
        private DateTime examStartTime;
        private int remainingMinutes;
        private int remainingSeconds;
        private bool isTimeUp = false;

        protected override async Task OnInitializedAsync()
        {
            await InitializeTimerAsync();
        }

        public async Task InitializeTimerAsync()
        {
            if (TotalDuration > 0)
            {
                // Kiểm tra xem có thời gian đã lưu trong localStorage không
                var savedTimerData = await JSRuntime.InvokeAsync<string>("localStorage.getItem", TIMER_STORAGE_KEY);
                
                if (!string.IsNullOrEmpty(savedTimerData))
                {
                    try
                    {
                        // Parse dữ liệu timer đã lưu
                        var timerData = JsonSerializer.Deserialize<ExamTimerData>(savedTimerData);
                        
                        if (timerData != null && timerData.StudentExamSessionId == StudentExamSessionId)
                        {
                            // Tính thời gian còn lại dựa trên thời gian bắt đầu và thời gian đã trôi qua
                            var elapsedTime = DateTime.Now - timerData.StartTime;
                            var totalDuration = TotalDuration + ExtraMinutes;
                            var remainingTimeInMinutes = totalDuration - elapsedTime.TotalMinutes;
                            
                            if (remainingTimeInMinutes > 0)
                            {
                                // Khôi phục thời gian còn lại
                                remainingMinutes = (int)remainingTimeInMinutes;
                                remainingSeconds = Math.Max(0, (int)((remainingTimeInMinutes - remainingMinutes) * 60));
                                examStartTime = timerData.StartTime;
                                
                                // Start timer
                                StartTimer();
                                
                                // Notify parent component
                                await OnTimerInitialized.InvokeAsync(timerData);
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Nếu có lỗi parse, xóa dữ liệu cũ và khởi tạo mới
                        await JSRuntime.InvokeVoidAsync("localStorage.removeItem", TIMER_STORAGE_KEY);
                    }
                }
                
                // Khởi tạo timer mới
                remainingMinutes = TotalDuration + ExtraMinutes;
                remainingSeconds = 0;
                examStartTime = DateTime.Now;
                
                // Lưu thông tin timer vào localStorage
                await SaveTimerDataToLocalStorage();
                
                // Start timer
                StartTimer();
                
                // Notify parent component
                var newTimerData = new ExamTimerData
                {
                    StudentExamSessionId = StudentExamSessionId,
                    StartTime = examStartTime,
                    Duration = remainingMinutes + remainingSeconds / 60.0
                };
                await OnTimerInitialized.InvokeAsync(newTimerData);
            }
        }
        
        public void StartTimer()
        {
            // Start timer that ticks every second
            examTimer = new System.Timers.Timer(1000);
            examTimer.Elapsed += OnTimerElapsed;
            examTimer.Start();
        }
        
        public void StopTimer()
        {
            examTimer?.Stop();
        }
        
        private async Task SaveTimerDataToLocalStorage()
        {
            var timerData = new ExamTimerData
            {
                StudentExamSessionId = StudentExamSessionId,
                StartTime = examStartTime,
                Duration = remainingMinutes + remainingSeconds / 60.0
            };
            
            var jsonData = JsonSerializer.Serialize(timerData);
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", TIMER_STORAGE_KEY, jsonData);
        }

        private async void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (remainingSeconds > 0)
            {
                remainingSeconds--;
            }
            else if (remainingMinutes > 0)
            {
                remainingMinutes--;
                remainingSeconds = 59;
            }
            else
            {
                // Time is up
                isTimeUp = true;
                examTimer?.Stop();
                
                // Xóa dữ liệu timer khỏi localStorage khi hết thời gian
                await InvokeAsync(async () =>
                {
                    await JSRuntime.InvokeVoidAsync("localStorage.removeItem", TIMER_STORAGE_KEY);
                    await OnTimeUp.InvokeAsync();
                });
                return;
            }
            
            // Cập nhật localStorage mỗi 4 giây để tránh lag
            if (remainingSeconds % 4 == 0)
            {
                await InvokeAsync(async () =>
                {
                    await SaveTimerDataToLocalStorage();
                });
            }
            
            // Update UI mỗi giây
            await InvokeAsync(StateHasChanged);
        }

        public string GetFormattedTime()
        {
            if (isTimeUp)
                return "00:00";
                
            return $"{remainingMinutes:D2}:{remainingSeconds:D2}";
        }

        public void Dispose()
        {
            examTimer?.Stop();
            examTimer?.Dispose();
            
            // Xóa dữ liệu timer khỏi localStorage khi dispose component
            _ = Task.Run(async () =>
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("localStorage.removeItem", TIMER_STORAGE_KEY);
                }
                catch
                {
                    // Ignore errors during disposal
                }
            });
        }
    }
}
