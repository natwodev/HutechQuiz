using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Text.RegularExpressions;
using System.Timers;
using frontend_manage.DTOs;
using frontend_manage.Services;

namespace frontend_manage.Pages.Exam
{
    // Class để lưu trữ thông tin timer
    public class ExamTimerData
    {
        public int StudentExamSessionId { get; set; }
        public DateTime StartTime { get; set; }
        public double Duration { get; set; } // Thời gian còn lại tính bằng phút
    }
    
    // Class để handle delayed save callback từ JavaScript
    public class DelayedSaveCallback : IDisposable
    {
        private readonly Exam _examComponent;
        private readonly int _order;
        private readonly int? _subOrder;
        private readonly string _answer;

        public DelayedSaveCallback(Exam examComponent, int order, int? subOrder, string answer)
        {
            _examComponent = examComponent;
            _order = order;
            _subOrder = subOrder;
            _answer = answer;
        }

        [JSInvokable]
        public async Task ExecuteSave()
        {
            await _examComponent.SaveAnswerSilentlyAsync(_order, _subOrder, _answer);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }

    public partial class Exam
    {
        [Parameter]
        [SupplyParameterFromQuery]
        public int? studentExamSessionId { get; set; }

        private StartExamResponseDto? startExamResponse;
        private ShuffledExamPaperDto? shuffledExam;
        private StudentExamSessionCacheDto? studentSession;
        private string? errorMessage;
        
        // Dictionary to store selected answers for each question
        private Dictionary<int, string> selectedAnswers = new();
        private Dictionary<int, string> selectedChildAnswers = new();
        
        // Dictionary to track saving state for each question
        private Dictionary<string, bool> savingStates = new();
        
        private DateTime? lastSaveTime = null;
        private int autoRefreshFailCount = 0;
        
        // Timer variables
        private System.Timers.Timer? examTimer;
        private System.Timers.Timer? autoSaveTimer;
        private int remainingMinutes;
        private int remainingSeconds;
        private bool isTimeUp = false;
        private DateTime examStartTime;
        private const string TIMER_STORAGE_KEY = "exam_timer_data";

        protected override async Task OnInitializedAsync()
        {
            // Khởi tạo rõ ràng các dictionary trống
            selectedAnswers = new Dictionary<int, string>();
            selectedChildAnswers = new Dictionary<int, string>();
            savingStates = new Dictionary<string, bool>();
            
            if (studentExamSessionId == null)
            {
                errorMessage = "Không tìm thấy thông tin ca thi môn học.";
                return;
            }
            try
            {
                startExamResponse = await StudentService.StartExamAsync(studentExamSessionId.Value);
                if (startExamResponse == null)
                {
                    errorMessage = "Không thể lấy thông tin làm bài.";
                }
                else
                {
                    shuffledExam = startExamResponse.ExamPaper;
                    studentSession = startExamResponse.StudentSession;
                    
                    // Load existing answers if any - chỉ sau khi đã có dữ liệu exam
                    
                    // Initialize timer
                    await InitializeTimerAsync();
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
        }

        private async Task OnAnswerSelected(int questionId, string answer, int order)
        {
            // Cập nhật selected answer ngay lập tức
            var answerValue = $"q{questionId}_{answer switch { "A" => "1", "B" => "2", "C" => "3", "D" => "4", _ => "1" }}";
            selectedAnswers[questionId] = answerValue;
            
            // Update UI ngay lập tức
            StateHasChanged();
            
            // Sử dụng debouncing để delay API call
            await JSRuntime.InvokeVoidAsync("window.answerDebouncer.debounce", 
                $"q{questionId}", 
                DotNetObjectReference.Create(new DelayedSaveCallback(this, order, null, answer)));
        }

        private async Task OnChildAnswerSelected(int questionId, string answer, int parentOrder, int childOrder)
        {
            // Cập nhật selected child answer ngay lập tức
            var answerValue = $"cq{questionId}_{answer switch { "A" => "1", "B" => "2", "C" => "3", "D" => "4", _ => "1" }}";
            selectedChildAnswers[questionId] = answerValue;
            
            // Update UI ngay lập tức
            StateHasChanged();
            
            // Sử dụng debouncing để delay API call
            await JSRuntime.InvokeVoidAsync("window.answerDebouncer.debounce", 
                $"cq{questionId}", 
                DotNetObjectReference.Create(new DelayedSaveCallback(this, parentOrder, childOrder, answer)));
        }

        public async Task SaveAnswerSilentlyAsync(int index, int? subIndex, string answer)
        {
            var questionKey = subIndex.HasValue ? $"{index}.{subIndex}" : index.ToString();
            
            try
            {
                // Set saving state (không cần StateHasChanged vì đây là background task)
                savingStates[questionKey] = true;
                
                var success = await SaveAnswerAsync(index, subIndex, answer);
                
                // Remove saving state
                savingStates.Remove(questionKey);
                
                if (!success)
                {
                    // Chỉ hiện thông báo lỗi nếu thất bại, không retry tự động để tránh spam
                    await InvokeAsync(() =>
                    {
                        Snackbar.Add($"Lưu đáp án câu {questionKey} thất bại. Hãy thử lại!", Severity.Warning, config =>
                        {
                            config.ShowCloseIcon = true;
                            config.VisibleStateDuration = 3000;
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                savingStates.Remove(questionKey);
                
                // Hiện thông báo lỗi nhẹ nhàng
                await InvokeAsync(() =>
                {
                    Snackbar.Add($"Mất kết nối - đáp án câu {questionKey} chưa được lưu", Severity.Error, config =>
                    {
                        config.ShowCloseIcon = true;
                        config.VisibleStateDuration = 5000;
                    });
                });
            }
        }

        private async Task SaveAnswerWithRetryAsync(int index, int? subIndex, string answer, int maxRetries = 2)
        {
            var questionKey = subIndex.HasValue ? $"{index}.{subIndex}" : index.ToString();
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var success = await SaveAnswerAsync(index, subIndex, answer);
                    if (success)
                    {
                        return; // Thành công, thoát
                    }
                }
                catch (Exception ex)
                {
                    // Attempt failed, continue to next attempt
                }
                
                if (attempt < maxRetries)
                {
                    // Wait before retry (shorter delay)
                    await Task.Delay(1000);
                }
            }
            
            // TODO: Thông báo lỗi lưu đáp án đã được gỡ bỏ
            // All attempts failed - but we no longer save answers via API
        }

        private async Task<bool> SaveAnswerAsync(int index, int? subIndex, string answer)
        {
            if (studentExamSessionId == null || studentSession == null)
            {
                return false;
            }

            try
            {
                // TODO: API lưu đáp án đã được gỡ bỏ - chờ hướng dẫn thêm
                
                // Tạm thời return true để không gây lỗi UI
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                throw; // Re-throw to be caught by retry logic
            }
        }

        private string GetSelectedAnswer(int questionId)
        {
            var result = selectedAnswers.TryGetValue(questionId, out var answer) ? answer : "";
            return result;
        }
        
        private string GetAnswerLabel(int order)
        {
            return order switch
            {
                1 => "A",
                2 => "B", 
                3 => "C",
                4 => "D",
                _ => order.ToString()
            };
        }

        private string GetSelectedChildAnswer(int questionId)
        {
            var result = selectedChildAnswers.TryGetValue(questionId, out var answer) ? answer : "";
            return result;
        }

        

        private Dictionary<string, string> ParseAnswersString(string answersString)
        {
            var result = new Dictionary<string, string>();
            
            if (string.IsNullOrEmpty(answersString))
                return result;

            try
            {
                var parts = answersString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var part in parts)
                {
                    var cleanPart = part.Trim('(', ')', ' ');
                    if (string.IsNullOrEmpty(cleanPart))
                        continue;
                        
                    var subParts = cleanPart.Split(',', 2);
                    
                    if (subParts.Length == 2)
                    {
                        var key = subParts[0].Trim();
                        var answer = subParts[1].Trim();
                        
                        // Validate answer is A, B, C, or D
                        if (!string.IsNullOrEmpty(key) && 
                            !string.IsNullOrEmpty(answer) && 
                            (answer == "A" || answer == "B" || answer == "C" || answer == "D"))
                        {
                            result[key] = answer;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Error parsing answers string
            }
            
            return result;
        }

        private string GetAnswerNumber(string answer)
        {
            return answer switch
            {
                "A" => "1",
                "B" => "2",
                "C" => "3",
                "D" => "4",
                _ => "" // Trả về chuỗi rỗng thay vì "1" để tránh tự động chọn đáp án A
            };
        }

        private bool IsQuestionSaving(int index, int? subIndex = null)
        {
            var questionKey = subIndex.HasValue ? $"{index}.{subIndex}" : index.ToString();
            return savingStates.ContainsKey(questionKey);
        }

        private async Task RefreshAnswersAsync(bool showSuccessMessage = true)
        {
            if (studentExamSessionId == null || studentSession == null)
                return;

            try
            {
                // Sử dụng StudentAnswersString có sẵn thay vì gọi API
                var answersString = studentSession.StudentAnswersString;
                if (!string.IsNullOrEmpty(answersString))
                {
                    // Clear current answers để reload từ StudentAnswersString
                    selectedAnswers.Clear();
                    selectedChildAnswers.Clear();
                    
                    // Load the answers từ StudentAnswersString có sẵn
                    
                    if (showSuccessMessage)
                    {
                        Snackbar.Add("Đã cập nhật đáp án từ dữ liệu hiện tại", Severity.Success, config =>
                        {
                            config.VisibleStateDuration = 2000;
                        });
                    }
                }
                else
                {
                    if (showSuccessMessage)
                    {
                        Snackbar.Add("Chưa có đáp án nào được lưu", Severity.Info, config =>
                        {
                            config.VisibleStateDuration = 2000;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (showSuccessMessage) // Chỉ hiển thị lỗi khi user click manual
                {
                    Snackbar.Add("Lỗi khi tải lại đáp án", Severity.Error);
                }
            }
        }

       private async Task OnSubmitExamAsync()
       {
           // Lấy chuỗi thời gian dạng "mm:ss"
           var formattedTime = GetFormattedTime();
           // Tách phút (phần trước dấu :)
           var minutesOnly = formattedTime.Split(':')[0];
           // Tạo message chỉ với phút
           var timeMessage = $"⏰ Thời gian còn lại: {minutesOnly} phút";

           var dialog = await Dialog.ShowMessageBox("Bạn có chắc chắn muốn nộp bài thi này?", timeMessage, "Nộp bài", "Hủy");
           if (dialog == true)
           {
               try
               {
                   // Stop timer chỉ khi thực sự nộp bài
                   examTimer?.Stop();
                   
                   // Xóa dữ liệu timer khỏi localStorage khi nộp bài
                   await JSRuntime.InvokeVoidAsync("localStorage.removeItem", TIMER_STORAGE_KEY);
                   
                   // TODO: Logic lưu đáp án cuối cùng đã được gỡ bỏ
                   
                   // TODO: API nộp bài đã được gỡ bỏ - chờ hướng dẫn thêm
                   
                   // Hiển thị thông báo tạm thời
                   Snackbar.Add("Chức năng nộp bài tạm thời không khả dụng", Severity.Warning);
                   
                   // Tạm thời không chuyển trang
                   // Navigation.NavigateTo("/Exam/Result");
               }
               catch (Exception ex)
               {
                   Snackbar.Add("Có lỗi xảy ra khi nộp bài. Vui lòng thử lại!", Severity.Error);
               }
           }
       }

        private async Task ScrollToQuestionAsync(string questionIdentifier)
        {
            await JSRuntime.InvokeVoidAsync("scrollToElement", $"question-{questionIdentifier}");
        }

        private async Task InitializeTimerAsync()
        {
            if (studentSession?.Duration > 0)
            {
                // Kiểm tra xem có thời gian đã lưu trong localStorage không
                var savedTimerData = await JSRuntime.InvokeAsync<string>("localStorage.getItem", TIMER_STORAGE_KEY);
                
                if (!string.IsNullOrEmpty(savedTimerData))
                {
                    try
                    {
                        // Parse dữ liệu timer đã lưu
                        var timerData = System.Text.Json.JsonSerializer.Deserialize<ExamTimerData>(savedTimerData);
                        
                        if (timerData != null && timerData.StudentExamSessionId == studentExamSessionId)
                        {
                            // Tính thời gian còn lại dựa trên thời gian bắt đầu và thời gian đã trôi qua
                            var elapsedTime = DateTime.Now - timerData.StartTime;
                            var totalDuration = studentSession.Duration + studentSession.ExtraMinutes;
                            var remainingTimeInMinutes = totalDuration - elapsedTime.TotalMinutes;
                            
                            if (remainingTimeInMinutes > 0)
                            {
                                // Khôi phục thời gian còn lại
                                remainingMinutes = (int)remainingTimeInMinutes;
                                remainingSeconds = Math.Max(0, (int)((remainingTimeInMinutes - remainingMinutes) * 60));
                                examStartTime = timerData.StartTime;
                                
                                // Start timer
                                StartTimer();
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
                remainingMinutes = studentSession.Duration + studentSession.ExtraMinutes;
                remainingSeconds = 0;
                examStartTime = DateTime.Now;
                
                // Lưu thông tin timer vào localStorage
                await SaveTimerDataToLocalStorage();
                
                // Start timer
                StartTimer();
            }
        }
        
        private void StartTimer()
        {
            // Start timer that ticks every second
            examTimer = new System.Timers.Timer(1000);
            examTimer.Elapsed += OnTimerElapsed;
            examTimer.Start();
        }
        
        private async Task SaveTimerDataToLocalStorage()
        {
            var timerData = new ExamTimerData
            {
                StudentExamSessionId = studentExamSessionId.Value,
                StartTime = examStartTime,
                Duration = remainingMinutes + remainingSeconds / 60.0
            };
            
            var jsonData = System.Text.Json.JsonSerializer.Serialize(timerData);
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", TIMER_STORAGE_KEY, jsonData);
        }

        private void OnAutoSaveElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            InvokeAsync(async () =>
            {
                try
                {
                    // Skip auto-refresh nếu đã fail quá nhiều lần liên tiếp
                    if (autoRefreshFailCount >= 3)
                    {
                        return;
                    }

                    await RefreshAnswersAsync(false); // Silent refresh, không hiển thị thông báo
                    
                    // Reset fail count on success
                    autoRefreshFailCount = 0;
                    
                    // Cập nhật save time cho auto-refresh
                    lastSaveTime = DateTime.Now;
                    StateHasChanged(); // Update UI to show new save time
                }
                catch (Exception ex)
                {
                    autoRefreshFailCount++;
                    
                    if (autoRefreshFailCount >= 3)
                    {
                        // Auto-refresh disabled due to repeated failures. Manual refresh still available.
                    }
                }
            });
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
                });
                
                // Auto submit exam
                await InvokeAsync(async () =>
                {
                    await OnSubmitExamAsync();
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

        public void Dispose()
        {
            examTimer?.Stop();
            examTimer?.Dispose();
            
            autoSaveTimer?.Stop();
            autoSaveTimer?.Dispose();
            
            // Xóa dữ liệu timer khỏi localStorage khi dispose component
            // Sử dụng Task.Run để tránh async void
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

        private string GetFormattedTime()
        {
            if (isTimeUp)
                return "00:00";
                
            return $"{remainingMinutes:D2}:{remainingSeconds:D2}";
        }

       
        

        private string GetAudioPath(string audioFileName)
        {
            if (string.IsNullOrEmpty(shuffledExam?.ShuffledExamPaperCore) || string.IsNullOrEmpty(audioFileName))
                return string.Empty;

            // Lấy phần trước dấu _ từ ShuffledExamPaperCore
            var folderName = shuffledExam.ShuffledExamPaperCore.Split('_')[0];
            
            // Tạo đường dẫn audio trực tiếp tới file trong backend
            var baseAddress = Http.BaseAddress?.ToString() ?? "http://localhost:5163/";
            return $"{baseAddress}EPZ/{folderName}/{audioFileName}";
        }

        private string GetPhysicalAudioPath(string audioFileName)
        {
            if (string.IsNullOrEmpty(shuffledExam?.ShuffledExamPaperCore) || string.IsNullOrEmpty(audioFileName))
                return string.Empty;

            // Lấy phần trước dấu _ từ ShuffledExamPaperCore
            var folderName = shuffledExam.ShuffledExamPaperCore.Split('_')[0];
            
            // Tạo đường dẫn vật lý để kiểm tra file có tồn tại không
            var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            return Path.Combine(wwwrootPath, "EPZ", folderName, audioFileName);
        }

       private string ProcessQuestionContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Tìm và thay thế thẻ audio
            var audioPattern = @"<audio>([^<]+)</audio>";
            var match = Regex.Match(content, audioPattern);
            
            if (match.Success)
            {
                var audioPath = match.Groups[1].Value;
                var fullAudioPath = GetAudioPath(audioPath);
                // Tạo audioId dựa trên audioPath để đảm bảo tính nhất quán
                var audioId = $"audio_{audioPath.GetHashCode().ToString().Replace("-", "n")}";
                
                if (!string.IsNullOrEmpty(fullAudioPath))
                {
                    // Thay thế thẻ audio bằng button đơn giản
                    var audioButton = $@"
                    <div class=""audio-player mb-3"">
                        <audio id=""{audioId}"" style=""display: none;"">
                            <source src=""{fullAudioPath}"" type=""audio/mpeg"">
                        </audio>
                        <button class=""mud-button-root mud-button mud-button-filled mud-button-filled-primary mud-button-filled-size-medium mud-ripple"" 
                                onclick=""playAudioSimple('{audioId}', '{fullAudioPath}')"">
                            <span class=""mud-button-label"">
                                🔊 Phát audio (5/5)
                            </span>
                        </button>
                    </div>";
                    
                    return Regex.Replace(content, audioPattern, audioButton);
                }
            }
            
            return content;
        }
        

    }
}