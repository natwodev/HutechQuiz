using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Text.RegularExpressions;
using System.Timers;
using frontend_manage.DTOs;

namespace frontend_manage.Pages.Exam
{
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
                startExamResponse = await InfoApi.StartExamAsync(studentExamSessionId.Value);
                if (startExamResponse == null)
                {
                    errorMessage = "Không thể lấy thông tin làm bài.";
                }
                else
                {
                    shuffledExam = startExamResponse.ExamPaper;
                    studentSession = startExamResponse.StudentSession;
                    
                    // Load existing answers if any - chỉ sau khi đã có dữ liệu exam
                    await LoadExistingAnswersAsync();
                    
                    // Initialize timer
                    InitializeTimer();
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
            
            // All attempts failed - show error
            Snackbar.Add($"Không thể lưu đáp án câu {questionKey}. Vui lòng kiểm tra kết nối mạng.", Severity.Error, config =>
            {
                config.ShowCloseIcon = true;
                config.VisibleStateDuration = 5000;
            });
        }

        private async Task<bool> SaveAnswerAsync(int index, int? subIndex, string answer)
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
                    Index = index,
                    SubIndex = subIndex,
                    Answer = answer
                };

                var response = await ExamApi.SaveAnswerAsync(request);
                
                if (response?.Success == true)
                {
                    return true;
                }
                else
                {
                    return false;
                }
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

        private string GetSelectedChildAnswer(int questionId)
        {
            var result = selectedChildAnswers.TryGetValue(questionId, out var answer) ? answer : "";
            return result;
        }

        private async Task LoadExistingAnswersAsync()
        {
            if (studentSession?.StudentAnswersString == null || shuffledExam?.Details == null)
                return;

            // Backup current state trước khi load
            var backupSelectedAnswers = new Dictionary<int, string>(selectedAnswers);
            var backupSelectedChildAnswers = new Dictionary<int, string>(selectedChildAnswers);

            try
            {
                // Parse existing answers string
                var answers = ParseAnswersString(studentSession.StudentAnswersString);
                
                // Chỉ proceed nếu có đáp án thực sự
                if (answers.Count == 0)
                {
                    return;
                }
                
                // Map answers to UI state
                int successfullyLoaded = 0;
                foreach (var detail in shuffledExam.Details.OrderBy(d => d.Order))
                {
                    // Sử dụng detail.Order thay vì IndexOf để đồng nhất với logic save
                    var questionNumber = detail.Order;
                    
                    // Check for main question answer
                    if (answers.TryGetValue(questionNumber.ToString(), out var mainAnswer))
                    {
                        var answerNumber = GetAnswerNumber(mainAnswer);
                        if (!string.IsNullOrEmpty(answerNumber)) // Chỉ set khi có đáp án hợp lệ
                        {
                            selectedAnswers[detail.ShuffledExamPaperDetailId] = $"q{detail.ShuffledExamPaperDetailId}_{answerNumber}";
                            successfullyLoaded++;
                        }
                    }
                    
                    // Check for child questions answers
                    if (detail.ChildQuestions != null)
                    {
                        foreach (var childQ in detail.ChildQuestions.OrderBy(cq => cq.Order))
                        {
                            // Sử dụng childQ.Order thay vì IndexOf
                            var childQuestionIndex = childQ.Order;
                            var childKey = $"{questionNumber}.{childQuestionIndex}";
                            
                            if (answers.TryGetValue(childKey, out var childAnswer))
                            {
                                var childAnswerNumber = GetAnswerNumber(childAnswer);
                                if (!string.IsNullOrEmpty(childAnswerNumber)) // Chỉ set khi có đáp án hợp lệ
                                {
                                    selectedChildAnswers[childQ.ShuffledExamPaperDetailId] = $"cq{childQ.ShuffledExamPaperDetailId}_{childAnswerNumber}";
                                    successfullyLoaded++;
                                }
                            }
                        }
                    }
                }
                
                StateHasChanged();
            }
            catch (Exception ex)
            {
                // Khôi phục trạng thái trước đó nếu có lỗi
                selectedAnswers = backupSelectedAnswers ?? selectedAnswers;
                selectedChildAnswers = backupSelectedChildAnswers ?? selectedChildAnswers;
                StateHasChanged();
            }
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
                    await LoadExistingAnswersAsync();
                    
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
           // Stop timer
           examTimer?.Stop();

           var dialog = await Dialog.ShowMessageBox("Xác nhận", "Bạn có chắc chắn muốn nộp bài thi này?", "Nộp bài", "Hủy");
           if (dialog == true)
           {
               try
               {
                   // Force save tất cả pending answers trước khi submit
                   var hasPending = await JSRuntime.InvokeAsync<bool>("window.answerDebouncer.hasPendingSaves");
                   if (hasPending)
                   {
                       Snackbar.Add("Đang lưu các đáp án cuối cùng...", Severity.Info);

                       // Đợi một chút để các pending saves hoàn thành
                       await Task.Delay(100);
                   }
                   
                   var submitRequest = new SubmitExamRequest
                   {
                       StudentExamSessionId = studentExamSessionId.Value
                   };
                       
                   var submitResponse = await ExamApi.SubmitExamAsync(submitRequest);

                   if (submitResponse?.Success == true && submitResponse.Data != null)
                   {
                       var submissionData = submitResponse.Data;

                       // Hiển thị thông báo thành công
                       var scoreMessage = $"Nộp bài thi thành công! Điểm: {submissionData.Score:F2}, Đúng: {submissionData.CorrectAnswers}/{submissionData.TotalQuestions} câu";
                       Snackbar.Add(scoreMessage, Severity.Success);

                       // Lưu dữ liệu kết quả vào localStorage
                       var resultData = new
                       {
                           StudentCode = submissionData.StudentCode,
                           ShuffledExamPaperId = submissionData.ShuffledExamPaperId,
                           Score = submissionData.Score,
                           CorrectAnswers = submissionData.CorrectAnswers,
                           TotalQuestions = submissionData.TotalQuestions,
                           EndTime = submissionData.EndTime,
                           StudentAnswersString = submissionData.StudentAnswersString,
                           AnswerKey = submissionData.AnswerKey
                       };

                       var resultJson = System.Text.Json.JsonSerializer.Serialize(resultData);
                       await JSRuntime.InvokeVoidAsync("localStorage.setItem", $"examResult_{studentExamSessionId}", resultJson);

                       // Lưu studentExamSessionId vào localStorage để Result page có thể truy cập
                       await JSRuntime.InvokeVoidAsync("localStorage.setItem", "currentStudentExamSessionId", studentExamSessionId.ToString());

                       // Chuyển sang trang Result
                       Navigation.NavigateTo("/Exam/Result");
                   }
                   else
                   {
                       var errorMessage = submitResponse?.Message ?? "Có lỗi xảy ra khi nộp bài thi";
                       Snackbar.Add(errorMessage, Severity.Error);
                   }
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

        private void InitializeTimer()
        {
            if (studentSession?.Duration > 0)
            {
                remainingMinutes = studentSession.Duration;
                remainingSeconds = 0;
                
                // Start timer that ticks every second
                examTimer = new System.Timers.Timer(1000);
                examTimer.Elapsed += OnTimerElapsed;
                examTimer.Start();
                
                // BỎ AUTO-SAVE TIMER - Chỉ dùng manual save
            }
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

        private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
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
                
                // Auto submit exam
                InvokeAsync(async () =>
                {
                    await OnSubmitExamAsync();
                });
            }
            
            // Update UI
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            examTimer?.Stop();
            examTimer?.Dispose();
            
            autoSaveTimer?.Stop();
            autoSaveTimer?.Dispose();
        }

        private string GetFormattedTime()
        {
            if (isTimeUp)
                return "00:00";
                
            return $"{remainingMinutes:D2}:{remainingSeconds:D2}";
        }

        private int GetAnsweredQuestionsCount()
        {
            if (shuffledExam?.Details == null) return 0;
            
            int answeredCount = 0;
            
            foreach (var detail in shuffledExam.Details)
            {
                // Nếu có câu hỏi con, chỉ đếm câu hỏi con đã trả lời (không đếm câu cha)
                if (detail.ChildQuestions != null && detail.ChildQuestions.Any())
                {
                    foreach (var childQ in detail.ChildQuestions)
                    {
                        if (!string.IsNullOrEmpty(GetSelectedChildAnswer(childQ.ShuffledExamPaperDetailId)))
                        {
                            answeredCount++;
                        }
                    }
                }
                else
                {
                    // Nếu không có câu hỏi con, đếm câu chính
                    if (!string.IsNullOrEmpty(GetSelectedAnswer(detail.ShuffledExamPaperDetailId)))
                    {
                        answeredCount++;
                    }
                }
            }
            
            return answeredCount;
        }

        private string GetQuestionCountDetails()
        {
            if (shuffledExam?.Details == null) return "Chưa có dữ liệu";
            
            int questionGroupsWithChildren = 0;
            int standaloneQuestions = 0;
            int totalAnswerableQuestions = 0;
            
            foreach (var detail in shuffledExam.Details)
            {
                if (detail.ChildQuestions != null && detail.ChildQuestions.Any())
                {
                    questionGroupsWithChildren++;
                    totalAnswerableQuestions += detail.ChildQuestions.Count;
                }
                else
                {
                    standaloneQuestions++;
                    totalAnswerableQuestions++;
                }
            }
            
            if (questionGroupsWithChildren > 0 && standaloneQuestions > 0)
            {
                return $"{standaloneQuestions} câu đơn + {questionGroupsWithChildren} nhóm câu = {totalAnswerableQuestions} câu trả lời";
            }
            else if (questionGroupsWithChildren > 0)
            {
                return $"{questionGroupsWithChildren} nhóm câu = {totalAnswerableQuestions} câu trả lời";
            }
            else
            {
                return $"{standaloneQuestions} câu đơn";
            }
        }

        private int GetTotalQuestionsCount()
        {
            if (shuffledExam?.Details == null) return 0;
            
            int total = 0;
            
            foreach (var detail in shuffledExam.Details)
            {
                // Nếu có câu hỏi con, chỉ đếm câu hỏi con (không đếm câu cha)
                if (detail.ChildQuestions != null && detail.ChildQuestions.Any())
                {
                    total += detail.ChildQuestions.Count;
                }
                else
                {
                    // Nếu không có câu hỏi con, đếm câu chính
                    total++;
                }
            }
            
            return total;
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
                
                if (!string.IsNullOrEmpty(fullAudioPath))
                {
                    // Thay thế thẻ audio bằng HTML audio player
                    var audioPlayer = $@"<div class=""audio-player mb-3"">
                        <audio controls style=""width: 100%; max-width: 400px;"">
                            <source src=""{fullAudioPath}"" type=""audio/mpeg"">
                            Your browser does not support the audio element.
                        </audio>
                    </div>";
                    
                    return Regex.Replace(content, audioPattern, audioPlayer);
                }
            }
            
            return content;
        }

        private string GetAnswerFromValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            
            // Extract the answer number from the value (e.g., "q123_1" -> "1")
            var parts = value.Split('_');
            if (parts.Length >= 2)
            {
                var answerNumber = parts[1];
                return answerNumber switch
                {
                    "1" => "A",
                    "2" => "B", 
                    "3" => "C",
                    "4" => "D",
                    _ => ""
                };
            }
            return "";
        }
    }
}