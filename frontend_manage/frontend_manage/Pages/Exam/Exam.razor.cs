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
        
        // Progress saving state
        private bool isSavingProgress = false;
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
            Console.WriteLine($"🔄 OnAnswerSelected: questionId={questionId}, answer={answer}, order={order}");
            
            // Cập nhật selected answer ngay lập tức
            var answerValue = $"q{questionId}_{answer switch { "A" => "1", "B" => "2", "C" => "3", "D" => "4", _ => "1" }}";
            selectedAnswers[questionId] = answerValue;
            Console.WriteLine($"📝 Set selectedAnswers[{questionId}] = {answerValue}");
            
            // Update UI ngay lập tức
            StateHasChanged();
            
            // Sử dụng debouncing để delay API call
            await JSRuntime.InvokeVoidAsync("window.answerDebouncer.debounce", 
                $"q{questionId}", 
                DotNetObjectReference.Create(new DelayedSaveCallback(this, order, null, answer)));
        }

        private async Task OnChildAnswerSelected(int questionId, string answer, int parentOrder, int childOrder)
        {
            Console.WriteLine($"🔄 OnChildAnswerSelected: questionId={questionId}, answer={answer}, parentOrder={parentOrder}, childOrder={childOrder}");
            
            // Cập nhật selected child answer ngay lập tức
            var answerValue = $"cq{questionId}_{answer switch { "A" => "1", "B" => "2", "C" => "3", "D" => "4", _ => "1" }}";
            selectedChildAnswers[questionId] = answerValue;
            Console.WriteLine($"📝 Set selectedChildAnswers[{questionId}] = {answerValue}");
            
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
                Console.WriteLine($"❌ Error saving answer silently: {ex.Message}");
                
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
                    Console.WriteLine($"❌ Attempt {attempt}/{maxRetries} failed: {ex.Message}");
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
                Console.WriteLine("❌ StudentExamSessionId hoặc studentSession là null");
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

                Console.WriteLine($"🔄 Đang gọi API save-answer: StudentExamSessionId={request.StudentExamSessionId}, Index={request.Index}, SubIndex={request.SubIndex}, Answer={request.Answer}");

                var response = await ExamApi.SaveAnswerAsync(request);
                
                if (response?.Success == true)
                {
                    Console.WriteLine($"✅ Đã lưu đáp án thành công: Câu {index}{(subIndex.HasValue ? $".{subIndex}" : "")} = {answer}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Lỗi khi lưu đáp án: {response?.Message ?? "Không xác định"}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception khi lưu đáp án: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                throw; // Re-throw to be caught by retry logic
            }
        }

        private string GetSelectedAnswer(int questionId)
        {
            var result = selectedAnswers.TryGetValue(questionId, out var answer) ? answer : "";
            Console.WriteLine($"🔍 GetSelectedAnswer({questionId}) = '{result}'");
            return result;
        }

        private string GetSelectedChildAnswer(int questionId)
        {
            var result = selectedChildAnswers.TryGetValue(questionId, out var answer) ? answer : "";
            Console.WriteLine($"🔍 GetSelectedChildAnswer({questionId}) = '{result}'");
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
                Console.WriteLine($"🔄 Loading existing answers: {studentSession.StudentAnswersString}");
                
                // Parse existing answers string
                var answers = ParseAnswersString(studentSession.StudentAnswersString);
                
                // Chỉ proceed nếu có đáp án thực sự
                if (answers.Count == 0)
                {
                    Console.WriteLine("ℹ️ No existing answers found or failed to parse");
                    return;
                }
                
                // Map answers to UI state
                int successfullyLoaded = 0;
                foreach (var detail in shuffledExam.Details.OrderBy(d => d.Order))
                {
                    // Sử dụng detail.Order thay vì IndexOf để đồng nhất với logic save
                    var questionNumber = detail.Order;
                    
                    Console.WriteLine($"🔍 Processing question {questionNumber} (ID: {detail.ShuffledExamPaperDetailId})");
                    
                    // Check for main question answer
                    if (answers.TryGetValue(questionNumber.ToString(), out var mainAnswer))
                    {
                        var answerNumber = GetAnswerNumber(mainAnswer);
                        if (!string.IsNullOrEmpty(answerNumber)) // Chỉ set khi có đáp án hợp lệ
                        {
                            selectedAnswers[detail.ShuffledExamPaperDetailId] = $"q{detail.ShuffledExamPaperDetailId}_{answerNumber}";
                            successfullyLoaded++;
                            Console.WriteLine($"✅ Loaded main answer for question {questionNumber} (ID: {detail.ShuffledExamPaperDetailId}): {mainAnswer}");
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
                            
                            Console.WriteLine($"🔍 Processing child question {childKey} (ID: {childQ.ShuffledExamPaperDetailId})");
                            
                            if (answers.TryGetValue(childKey, out var childAnswer))
                            {
                                var childAnswerNumber = GetAnswerNumber(childAnswer);
                                if (!string.IsNullOrEmpty(childAnswerNumber)) // Chỉ set khi có đáp án hợp lệ
                                {
                                    selectedChildAnswers[childQ.ShuffledExamPaperDetailId] = $"cq{childQ.ShuffledExamPaperDetailId}_{childAnswerNumber}";
                                    successfullyLoaded++;
                                    Console.WriteLine($"✅ Loaded child answer for question {childKey} (ID: {childQ.ShuffledExamPaperDetailId}): {childAnswer}");
                                }
                            }
                        }
                    }
                }
                
                Console.WriteLine($"📊 Successfully loaded {successfullyLoaded} answers from server");
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading existing answers: {ex.Message}");
                Console.WriteLine($"🔄 Restoring previous state due to error");
                
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
                            Console.WriteLine($"📝 Parsed answer: {key} = {answer}");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Invalid answer format ignored: {cleanPart}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error parsing answers string: {ex.Message}");
            }
            
            Console.WriteLine($"📊 Total parsed answers: {result.Count}");
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

        private async Task SaveProgressAsync()
        {
            if (isSavingProgress || studentExamSessionId == null)
                return;

            try
            {
                isSavingProgress = true;
                StateHasChanged();

                Console.WriteLine("💾 Starting manual save progress...");

                // Collect all current answers to save
                var saveTasks = new List<Task<bool>>();
                var answersToSave = new List<(int index, int? subIndex, string answer)>();

                // Collect main question answers
                if (shuffledExam?.Details != null)
                {
                    foreach (var detail in shuffledExam.Details.OrderBy(d => d.Order))
                    {
                        var questionNumber = shuffledExam.Details.OrderBy(d => d.Order).ToList().IndexOf(detail) + 1;
                        var selectedAnswer = GetSelectedAnswer(detail.ShuffledExamPaperDetailId);
                        
                        if (!string.IsNullOrEmpty(selectedAnswer))
                        {
                            var answer = GetAnswerFromValue(selectedAnswer);
                            if (!string.IsNullOrEmpty(answer))
                            {
                                answersToSave.Add((questionNumber, null, answer));
                            }
                        }

                        // Collect child question answers
                        if (detail.ChildQuestions != null)
                        {
                            foreach (var childQ in detail.ChildQuestions.OrderBy(cq => cq.Order))
                            {
                                var childQuestionIndex = detail.ChildQuestions.OrderBy(cq => cq.Order).ToList().IndexOf(childQ) + 1;
                                var selectedChildAnswer = GetSelectedChildAnswer(childQ.ShuffledExamPaperDetailId);
                                
                                if (!string.IsNullOrEmpty(selectedChildAnswer))
                                {
                                    var answer = GetAnswerFromValue(selectedChildAnswer);
                                    if (!string.IsNullOrEmpty(answer))
                                    {
                                        answersToSave.Add((questionNumber, childQuestionIndex, answer));
                                    }
                                }
                            }
                        }
                    }
                }

                Console.WriteLine($"💾 Found {answersToSave.Count} answers to save");

                if (answersToSave.Count == 0)
                {
                    Snackbar.Add("Chưa có đáp án nào để lưu", Severity.Info, config =>
                    {
                        config.VisibleStateDuration = 2000;
                    });
                    return;
                }

                // Save all answers
                int successCount = 0;
                foreach (var (index, subIndex, answer) in answersToSave)
                {
                    try
                    {
                        var success = await SaveAnswerAsync(index, subIndex, answer);
                        if (success) successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Failed to save answer {index}{(subIndex.HasValue ? $".{subIndex}" : "")}: {ex.Message}");
                    }
                }

                // Show result
                if (successCount == answersToSave.Count)
                {
                    lastSaveTime = DateTime.Now;
                    Snackbar.Add($"✅ Đã lưu thành công {successCount}/{answersToSave.Count} đáp án", Severity.Success, config =>
                    {
                        config.VisibleStateDuration = 3000;
                    });
                }
                else if (successCount > 0)
                {
                    lastSaveTime = DateTime.Now;
                    Snackbar.Add($"⚠️ Đã lưu {successCount}/{answersToSave.Count} đáp án. Một số đáp án không lưu được.", Severity.Warning, config =>
                    {
                        config.VisibleStateDuration = 4000;
                    });
                }
                else
                {
                    Snackbar.Add("❌ Không thể lưu đáp án. Vui lòng kiểm tra kết nối mạng.", Severity.Error, config =>
                    {
                        config.VisibleStateDuration = 5000;
                    });
                }

                Console.WriteLine($"💾 Save progress completed: {successCount}/{answersToSave.Count} successful");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during save progress: {ex.Message}");
                Snackbar.Add("Lỗi khi lưu bài thi. Vui lòng thử lại.", Severity.Error, config =>
                {
                    config.VisibleStateDuration = 5000;
                });
            }
            finally
            {
                isSavingProgress = false;
                StateHasChanged();
            }
        }

        private async Task RefreshAnswersAsync(bool showSuccessMessage = true)
        {
            if (studentExamSessionId == null)
                return;

            try
            {
                Console.WriteLine($"🔄 RefreshAnswersAsync called (showMessage: {showSuccessMessage})");
                Console.WriteLine($"📊 Current state - selectedAnswers: {selectedAnswers.Count}, selectedChildAnswers: {selectedChildAnswers.Count}");
                
                var answersString = await ExamApi.GetStudentAnswersAsync(studentExamSessionId.Value);
                if (!string.IsNullOrEmpty(answersString))
                {
                    // Kiểm tra xem có thay đổi không trước khi clear và reload
                    var currentAnswersString = studentSession?.StudentAnswersString ?? "";
                    
                    Console.WriteLine($"🔍 Comparing answers - Current: '{currentAnswersString}' vs Server: '{answersString}'");
                    
                    if (answersString != currentAnswersString)
                    {
                        Console.WriteLine($"🔄 Detected changes in answers. Updating...");
                        Console.WriteLine($"📊 Before clear - selectedAnswers: {selectedAnswers.Count}, selectedChildAnswers: {selectedChildAnswers.Count}");
                        
                        // Clear current answers only if there are actual changes
                        selectedAnswers.Clear();
                        selectedChildAnswers.Clear();
                        
                        Console.WriteLine($"📊 After clear - selectedAnswers: {selectedAnswers.Count}, selectedChildAnswers: {selectedChildAnswers.Count}");
                        
                        // Update student session with new answers
                        if (studentSession != null)
                        {
                            studentSession.StudentAnswersString = answersString;
                        }
                        
                        // Load the refreshed answers
                        await LoadExistingAnswersAsync();
                        
                        Console.WriteLine($"📊 After load - selectedAnswers: {selectedAnswers.Count}, selectedChildAnswers: {selectedChildAnswers.Count}");
                        Console.WriteLine("✅ Successfully refreshed answers with changes");
                        if (showSuccessMessage)
                        {
                            Snackbar.Add("Đã cập nhật đáp án từ server", Severity.Success, config =>
                            {
                                config.VisibleStateDuration = 2000;
                            });
                        }
                    }
                    else
                    {
                        Console.WriteLine("ℹ️ No changes detected in answers, skipping refresh");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ No answers found on server");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error refreshing answers: {ex.Message}");
                if (showSuccessMessage) // Chỉ hiển thị lỗi khi user click manual
                {
                    Snackbar.Add("Lỗi khi tải lại đáp án từ server", Severity.Error);
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
                        Console.WriteLine("💾 Force saving all pending answers before submit...");
                        Snackbar.Add("Đang lưu các đáp án cuối cùng...", Severity.Info);
                        
                        // Đợi một chút để các pending saves hoàn thành
                        await Task.Delay(100);
                        
                        // TODO: Có thể implement force save all nếu cần
                        Console.WriteLine("✅ All pending answers should be saved");
                    }
                    
                    // TODO: Implement submit exam logic
                    Snackbar.Add("Đã nộp bài thi thành công!", Severity.Success);
                    
                    // Chuyển sang trang Result
                    Navigation.NavigateTo($"/Exam/Result?studentExamSessionId={studentExamSessionId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error during submit: {ex.Message}");
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
                Console.WriteLine("ℹ️ Auto-refresh disabled - using manual save only");
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
                        Console.WriteLine($"🚫 Skipping auto-refresh due to {autoRefreshFailCount} consecutive failures");
                        return;
                    }

                    var oldAnswersString = studentSession?.StudentAnswersString ?? "";
                    await RefreshAnswersAsync(false); // Silent refresh, không hiển thị thông báo
                    
                    // Reset fail count on success
                    autoRefreshFailCount = 0;
                    
                    // Chỉ cập nhật save time nếu thực sự có thay đổi
                    var newAnswersString = studentSession?.StudentAnswersString ?? "";
                    if (newAnswersString != oldAnswersString)
                    {
                        lastSaveTime = DateTime.Now; // Update save time for auto-save only if changed
                        StateHasChanged(); // Update UI to show new save time
                        Console.WriteLine("🔄 Auto-save refresh completed with changes");
                    }
                    else
                    {
                        Console.WriteLine("🔄 Auto-save refresh completed - no changes");
                    }
                }
                catch (Exception ex)
                {
                    autoRefreshFailCount++;
                    Console.WriteLine($"❌ Auto-save refresh failed (attempt {autoRefreshFailCount}): {ex.Message}");
                    
                    if (autoRefreshFailCount >= 3)
                    {
                        Console.WriteLine("⚠️ Auto-refresh disabled due to repeated failures. Manual refresh still available.");
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
            
            Console.WriteLine($"📊 Answered questions count (answerable): {answeredCount}");
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
            
            Console.WriteLine($"📊 Total questions count (answerable): {total}");
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
                
                // Debug: Log đường dẫn audio
                Console.WriteLine($"Audio path: {audioPath}");
                Console.WriteLine($"Full audio path: {fullAudioPath}");
                
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