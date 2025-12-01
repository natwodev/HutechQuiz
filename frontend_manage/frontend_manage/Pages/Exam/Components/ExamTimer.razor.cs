using Microsoft.AspNetCore.Components;

namespace frontend_manage.Pages.Exam.Components
{
    public partial class ExamTimer : ComponentBase, IDisposable
    {
        [Parameter] public int DurationMinutes { get; set; }
        [Parameter] public int ExtraMinutes { get; set; }
        [Parameter] public DateTime StartTime { get; set; }
        [Parameter] public DateTime? ExamSessionStartTime { get; set; }
        [Parameter] public bool UseExamSessionFormula { get; set; }
        [Parameter] public EventCallback OnTimeUp { get; set; }

        private System.Timers.Timer? _timer;
        private TimeSpan _remaining = TimeSpan.Zero;

        protected double Progress => ComputeProgress();
        protected string RemainingString => _remaining.TotalSeconds <= 0
            ? "00:00:00"
            : $"{(int)_remaining.TotalHours:D2}:{_remaining.Minutes:D2}:{_remaining.Seconds:D2}";

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
            var total = ResolveTotalDuration();
            var elapsed = DateTime.Now - ResolveStartTime();
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
            var totalSeconds = ResolveTotalDuration().TotalSeconds;
            if (totalSeconds <= 0) return 0;

            var elapsedSeconds = (DateTime.Now - ResolveStartTime()).TotalSeconds;
            if (elapsedSeconds <= 0) return 0;
            if (elapsedSeconds >= totalSeconds) return 100;

            return elapsedSeconds / totalSeconds * 100d;
        }

        private TimeSpan ResolveTotalDuration()
        {
            var minutes = DurationMinutes;
            if (UseExamSessionFormula)
            {
                minutes += ExtraMinutes;
            }

            if (minutes < 0)
            {
                minutes = 0;
            }

            return TimeSpan.FromMinutes(minutes);
        }

        private DateTime ResolveStartTime()
        {
            if (UseExamSessionFormula && ExamSessionStartTime.HasValue)
            {
                return ExamSessionStartTime.Value;
            }

            return StartTime == default ? DateTime.Now : StartTime;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}


