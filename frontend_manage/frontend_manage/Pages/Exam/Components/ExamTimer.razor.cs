using Microsoft.AspNetCore.Components;

namespace frontend_manage.Pages.Exam.Components
{
    public partial class ExamTimer : ComponentBase, IDisposable
    {
        [Parameter] public int DurationMinutes { get; set; }
        [Parameter] public DateTime StartTime { get; set; }
        [Parameter] public EventCallback OnTimeUp { get; set; }

        private System.Timers.Timer? _timer;
        private TimeSpan _remaining = TimeSpan.Zero;

        protected double Progress => ComputeProgress();
        protected string RemainingString => _remaining.TotalSeconds < 0 ? "00:00:00" : $"{(int)_remaining.TotalHours:D2}:{_remaining.Minutes:D2}:{_remaining.Seconds:D2}";

        protected override void OnInitialized()
        {
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += (_, _) => Tick();
            _timer.AutoReset = true;
            _timer.Enabled = true;
            Tick();
        }

        private void Tick()
        {
            var total = TimeSpan.FromMinutes(DurationMinutes);
            var elapsed = DateTime.Now - StartTime;
            _remaining = total - elapsed;
            if (_remaining.TotalSeconds <= 0)
            {
                _remaining = TimeSpan.Zero;
                _ = OnTimeUp.InvokeAsync();
                _timer?.Stop();
            }
            InvokeAsync(StateHasChanged);
        }

        private double ComputeProgress()
        {
            var total = TimeSpan.FromMinutes(DurationMinutes).TotalSeconds;
            if (total <= 0) return 0;
            var elapsed = (DateTime.Now - StartTime).TotalSeconds;
            if (elapsed <= 0) return 0;
            if (elapsed >= total) return 100;
            return elapsed / total * 100d;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}


