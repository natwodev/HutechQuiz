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
    public partial class Exam
    {
        [Parameter]
        [SupplyParameterFromQuery]
        public int? studentExamSessionId { get; set; }

        private StartExamResponseDto? startExamResponse;
        private ShuffledExamPaperDto? shuffledExam;
        private OriginalExamPaperDto? originalExamPaper;
        private StudentExamSessionCacheDto? studentSession;
        private string? errorMessage;
        
        // Dữ liệu đã map từ originalExamPaper
        private List<QuestionStructureDto>? mappedQuestions;
        
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
                    originalExamPaper = startExamResponse.OriginalExamPaper;
                    studentSession = startExamResponse.StudentSession;
                    
                    // Map dữ liệu từ originalExamPaper sang QuestionStructureDto
                    if (originalExamPaper?.Details != null)
                    {
                        mappedQuestions = MapToQuestionStructureList(originalExamPaper.Details);
                    }
                    
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
        
        
        
        private async Task<bool> SaveAnswerAsync(int key, int value)
        {
            if (studentExamSessionId == null || studentSession == null)
            {
                return false;
            }

            try
            {
                var request = new SaveAnswerDto
                {
                    StudentExamSessionId = studentExamSessionId.Value,
                    key = key,
                    value = value,
                };

                var response = await StudentService.SaveAnswerAsync(request);
        
                if (response?.Success == true)
                {
                    // Cập nhật selectedAnswers để navigation panel hiển thị đúng
                    selectedAnswers[key] = value.ToString();
                    lastSaveTime = DateTime.Now;
                    StateHasChanged(); // Cập nhật UI
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                throw; // Re-throw để retry ở chỗ khác
            }
        }

        private async Task<bool> SaveChildAnswerAsync(int key, int value)
        {
            if (studentExamSessionId == null || studentSession == null)
            {
                return false;
            }

            try
            {
                var request = new SaveAnswerDto
                {
                    StudentExamSessionId = studentExamSessionId.Value,
                    key = key,
                    value = value,
                };

                var response = await StudentService.SaveAnswerAsync(request);
        
                if (response?.Success == true)
                {
                    // Cập nhật selectedChildAnswers để navigation panel hiển thị đúng
                    selectedChildAnswers[key] = value.ToString();
                    lastSaveTime = DateTime.Now;
                    StateHasChanged(); // Cập nhật UI
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                throw; // Re-throw để retry ở chỗ khác
            }
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
        


       
       
       
       
       
       
       
       
       
       
       
        private QuestionStructureDto MapToQuestionStructure(OriginalExamPaperDetailDto originalQuestion)
        {
            if (originalQuestion == null)
                return null;

            var questionStructure = new QuestionStructureDto
            {
                OriginalExamPaperDetailId = originalQuestion.OriginalExamPaperDetailId,
                ParentQuestionId = originalQuestion.ParentQuestionId,
                Order = originalQuestion.Order,
                QuestionContent = originalQuestion.QuestionContent,
                ChildQuestions = new List<QuestionStructureDto>(),
                Answers = new List<AnswerStructureDto>()
            };

            // Map child questions recursively
            if (originalQuestion.ChildQuestions?.Any() == true)
            {
                questionStructure.ChildQuestions = originalQuestion.ChildQuestions
                    .Select(child => MapToQuestionStructure(child))
                    .Where(child => child != null)
                    .ToList();
            }

            // Map answers
            if (originalQuestion.Answers?.Any() == true)
            {
                questionStructure.Answers = originalQuestion.Answers
                    .Select(answer => MapToAnswerStructure(answer))
                    .Where(answer => answer != null)
                    .ToList();
            }

            return questionStructure;
        }
        
        private AnswerStructureDto MapToAnswerStructure(AnswerDto originalAnswer)
        {
            if (originalAnswer == null)
                return null;

            return new AnswerStructureDto
            {
                AnswerId = originalAnswer.AnswerId,
                Order = originalAnswer.Order,
                AnswerContent = originalAnswer.AnswerContent,
                OriginalExamPaperDetailId = originalAnswer.OriginalExamPaperDetailId
            };
        }
        
        private List<QuestionStructureDto> MapToQuestionStructureList(List<OriginalExamPaperDetailDto> originalQuestions)
        {
            if (originalQuestions?.Any() != true)
                return new List<QuestionStructureDto>();

            return originalQuestions
                .Select(question => MapToQuestionStructure(question))
                .Where(question => question != null)
                .ToList();
        }

        // Navigation Panel Helper Methods
        private string GetSelectedAnswer(int originalExamPaperDetailId)
        {
            selectedAnswers.TryGetValue(originalExamPaperDetailId, out string? value);
            return value ?? string.Empty;
        }

        private string GetSelectedChildAnswer(int originalExamPaperDetailId)
        {
            selectedChildAnswers.TryGetValue(originalExamPaperDetailId, out string? value);
            return value ?? string.Empty;
        }

        private int GetAnsweredQuestionsCount()
        {
            if (mappedQuestions == null) return 0;

            int count = 0;
            foreach (var question in mappedQuestions.OrderBy(d => d.Order))
            {
                if (question.ChildQuestions?.Any() == true)
                {
                    // Câu hỏi cha có câu con - đếm câu con đã trả lời
                    count += question.ChildQuestions.Count(childQ => !string.IsNullOrEmpty(GetSelectedChildAnswer(childQ.OriginalExamPaperDetailId)));
                }
                else
                {
                    // Câu hỏi đơn lẻ
                    if (!string.IsNullOrEmpty(GetSelectedAnswer(question.OriginalExamPaperDetailId)))
                        count++;
                }
            }
            return count;
        }

        private int GetTotalQuestionsCount()
        {
            if (mappedQuestions == null) return 0;

            int count = 0;
            foreach (var question in mappedQuestions.OrderBy(d => d.Order))
            {
                if (question.ChildQuestions?.Any() == true)
                {
                    // Câu hỏi cha có câu con - đếm số câu con
                    count += question.ChildQuestions.Count;
                }
                else
                {
                    // Câu hỏi đơn lẻ
                    count++;
                }
            }
            return count;
        }

        private string GetQuestionCountDetails()
        {
            if (mappedQuestions == null) return "";

            int parentQuestions = mappedQuestions.Count(q => q.ChildQuestions?.Any() == true);
            int singleQuestions = mappedQuestions.Count(q => q.ChildQuestions?.Any() != true);
            int childQuestions = mappedQuestions.Where(q => q.ChildQuestions?.Any() == true)
                                               .Sum(q => q.ChildQuestions.Count);

            var details = new List<string>();
            if (singleQuestions > 0)
                details.Add($"{singleQuestions} câu đơn");
            if (parentQuestions > 0)
                details.Add($"{parentQuestions} câu cha");
            if (childQuestions > 0)
                details.Add($"{childQuestions} câu con");

            return string.Join(", ", details);
        }

        // Helper methods for RadioGroup values
        private int? GetSelectedAnswerAsInt(int originalExamPaperDetailId)
        {
            var selectedValue = GetSelectedAnswer(originalExamPaperDetailId);
            if (string.IsNullOrEmpty(selectedValue))
                return null;
            
            if (int.TryParse(selectedValue, out int result))
                return result;
            
            return null;
        }

        private int? GetSelectedChildAnswerAsInt(int originalExamPaperDetailId)
        {
            var selectedValue = GetSelectedChildAnswer(originalExamPaperDetailId);
            if (string.IsNullOrEmpty(selectedValue))
                return null;
            
            if (int.TryParse(selectedValue, out int result))
                return result;
            
            return null;
        }
    }
}