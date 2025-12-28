using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace frontend_manage.Pages.Exam.Components;

public partial class AudioPlayer : ComponentBase, IAsyncDisposable
{
    [Parameter] public string AudioUrl { get; set; } = string.Empty;
    [Parameter] public string AudioId { get; set; } = string.Empty;
    [Parameter] public string AudioPath { get; set; } = string.Empty;
    [Parameter] public int? StudentExamSessionId { get; set; }
    [Parameter] public int? QuestionId { get; set; }
    
    [Inject] private IJSRuntime JS { get; set; } = default!;
    
    private ElementReference _containerRef;
    private ElementReference _audioRef;
    private string BadgeText { get; set; } = "5/5";
    private System.Threading.Timer? _updateTimer;
    
    // Audio state
    private bool _isPlaying = false;
    private bool _isMuted = false;
    private double _currentTime = 0;
    private double _duration = 0;
    private double _volume = 100;
    private double _lastAllowedTime = 0; // Để chặn tua ngược
    private int _remainingCount = 5;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !string.IsNullOrEmpty(AudioId))
        {
            try
            {
                // Initialize audio element
                await JS.InvokeVoidAsync("eval", $@"
                    (function() {{
                        const audio = document.getElementById('{AudioId}');
                        if (audio) {{
                            // Set initial volume
                            audio.volume = {_volume / 100};
                            
                            // Attach event listeners
                            audio.addEventListener('play', function() {{
                                DotNet.invokeMethodAsync('frontend_manage', 'OnAudioPlay', '{AudioId}');
                            }});
                            
                            audio.addEventListener('pause', function() {{
                                DotNet.invokeMethodAsync('frontend_manage', 'OnAudioPause', '{AudioId}');
                            }});
                            
                            // Chặn tua ngược
                            let lastTime = 0;
                            audio.addEventListener('timeupdate', function() {{
                                if (audio.currentTime < lastTime - 0.3) {{
                                    audio.currentTime = lastTime;
                                }} else {{
                                    lastTime = audio.currentTime;
                                }}
                            }});
                        }}
                    }})()
                ");
                
                // Load play count
                if (StudentExamSessionId.HasValue && QuestionId.HasValue)
                {
                    await LoadPlayCountAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing audio player: {ex.Message}");
            }
        }
    }
    
    private async Task LoadPlayCountAsync()
    {
        if (!StudentExamSessionId.HasValue || string.IsNullOrEmpty(AudioId)) return;
        
        try
        {
            await JS.InvokeVoidAsync("loadAudioPlayCounts", StudentExamSessionId.Value);
            var count = await JS.InvokeAsync<int>("getAudioRemainingCount", AudioId);
            _remainingCount = count;
            BadgeText = $"{_remainingCount}/5";
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading play count: {ex.Message}");
        }
    }
    
    private async Task TogglePlay()
    {
        if (_remainingCount <= 0) return;
        
        try
        {
            var audio = await JS.InvokeAsync<IJSObjectReference>("eval", $@"
                (function() {{
                    return document.getElementById('{AudioId}');
                }})()
            ");
            
            if (audio != null)
            {
                if (_isPlaying)
                {
                    await JS.InvokeVoidAsync("eval", $"document.getElementById('{AudioId}').pause();");
                }
                else
                {
                    // Check play count before playing
                    if (StudentExamSessionId.HasValue && QuestionId.HasValue)
                    {
                        var currentCount = await JS.InvokeAsync<int>("eval", $@"
                            (function() {{
                                return window.audioPlayCounts['{AudioId}'] || 0;
                            }})()
                        ");
                        
                        if (currentCount >= 5)
                        {
                            return;
                        }
                        
                        // Increase count
                        var newCount = currentCount + 1;
                        await JS.InvokeVoidAsync("eval", $@"
                            (function() {{
                                window.audioPlayCounts = window.audioPlayCounts || {{}};
                                window.audioPlayCounts['{AudioId}'] = {newCount};
                                if ({StudentExamSessionId.Value} && {QuestionId.Value}) {{
                                    window.saveAudioPlayCount({StudentExamSessionId.Value}, {QuestionId.Value}, '{AudioId}', {newCount});
                                }}
                            }})()
                        ");
                        
                        _remainingCount = 5 - newCount;
                        BadgeText = $"{_remainingCount}/5";
                        _lastAllowedTime = 0; // Reset khi bắt đầu play mới
                    }
                    
                    await JS.InvokeVoidAsync("eval", $@"
                        (function() {{
                            const audio = document.getElementById('{AudioId}');
                            if (audio) {{
                                audio.currentTime = 0;
                                audio.play();
                            }}
                        }})()
                    ");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling play: {ex.Message}");
        }
    }
    
    private async Task ToggleMute()
    {
        _isMuted = !_isMuted;
        await JS.InvokeVoidAsync("eval", $@"
            (function() {{
                const audio = document.getElementById('{AudioId}');
                if (audio) {{
                    audio.muted = {(_isMuted ? "true" : "false")};
                }}
            }})()
        ");
        await InvokeAsync(StateHasChanged);
    }
    
    private async Task OnSliderChanged(ChangeEventArgs e)
    {
        if (e.Value == null) return;
        
        var newTime = Convert.ToDouble(e.Value);
        
        // Chỉ cho phép seek về sau, không cho seek về trước
        if (newTime < _lastAllowedTime - 0.3)
        {
            _currentTime = _lastAllowedTime;
            await InvokeAsync(StateHasChanged);
            return;
        }
        
        _currentTime = newTime;
        _lastAllowedTime = newTime;
        
        await JS.InvokeVoidAsync("eval", $@"
            (function() {{
                const audio = document.getElementById('{AudioId}');
                if (audio) {{
                    audio.currentTime = {newTime};
                }}
            }})()
        ");
    }
    
    private async Task OnVolumeChanged(ChangeEventArgs e)
    {
        if (e.Value == null) return;
        
        var newVolume = Convert.ToDouble(e.Value);
        _volume = newVolume;
        
        await JS.InvokeVoidAsync("eval", $@"
            (function() {{
                const audio = document.getElementById('{AudioId}');
                if (audio) {{
                    audio.volume = {newVolume / 100};
                }}
            }})()
        ");
    }
    
    private void OnPlay()
    {
        _isPlaying = true;
        InvokeAsync(StateHasChanged);
    }
    
    private void OnPause()
    {
        _isPlaying = false;
        InvokeAsync(StateHasChanged);
    }
    
    private void OnAudioLoaded()
    {
        // Load duration when metadata is loaded
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            var duration = await JS.InvokeAsync<double>("eval", $@"
                (function() {{
                    const audio = document.getElementById('{AudioId}');
                    return audio ? audio.duration : 0;
                }})()
            ");
            _duration = duration;
            await InvokeAsync(StateHasChanged);
        });
    }
    
    private void OnTimeUpdate()
    {
        // Update current time from audio element
        _ = Task.Run(async () =>
        {
            try
            {
                var currentTime = await JS.InvokeAsync<double>("eval", $@"
                    (function() {{
                        const audio = document.getElementById('{AudioId}');
                        return audio ? audio.currentTime : 0;
                    }})()
                ");
                
                // Chỉ cập nhật nếu không phải do user seek (để tránh conflict)
                if (Math.Abs(currentTime - _currentTime) < 1.0)
                {
                    _currentTime = currentTime;
                    if (currentTime > _lastAllowedTime)
                    {
                        _lastAllowedTime = currentTime;
                    }
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating time: {ex.Message}");
            }
        });
    }
    
    private void OnAudioEnded()
    {
        _isPlaying = false;
        _currentTime = 0;
        _lastAllowedTime = 0;
        InvokeAsync(StateHasChanged);
    }
    
    private string FormatTime(double seconds)
    {
        if (double.IsNaN(seconds) || seconds < 0) return "0:00";
        
        var minutes = (int)(seconds / 60);
        var secs = (int)(seconds % 60);
        return $"{minutes}:{secs:D2}";
    }
    
    [JSInvokable]
    public static void OnAudioPlay(string audioId)
    {
        // This will be called from JavaScript
    }
    
    [JSInvokable]
    public static void OnAudioPause(string audioId)
    {
        // This will be called from JavaScript
    }
    
    public async ValueTask DisposeAsync()
    {
        _updateTimer?.Dispose();
        await Task.CompletedTask;
    }
}

