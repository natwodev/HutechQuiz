using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Text.Json;

using System.Timers;
using frontend_manage.DTOs;
using frontend_manage.Services;
using frontend_manage.Pages.Exam.Components;
using frontend_manage.DTOs.Mapp;


namespace frontend_manage.Pages.Exam
{

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
        private System.Timers.Timer? autoSaveTimer;
        private int remainingMinutes;
        private int remainingSeconds;
        private bool isTimeUp = false;
        private DateTime examStartTime;

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
                        mappedQuestions = QuestionMapping.MapToQuestionStructureList(originalExamPaper.Details);
                    }
                    
                    // Load existing answers if any - chỉ sau khi đã có dữ liệu exam
                    if (!string.IsNullOrEmpty(studentSession.StudentAnswersString))
                    {
                        LoadAnswersFromString(studentSession.StudentAnswersString);
                        StateHasChanged(); // Cập nhật UI sau khi load answers
                    }
                    
                    // Initialize timer
                    await InitializeTimerAsync();
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
        }
        
        
        
        // Handler methods for QuestionItem component events
        private async Task HandleAnswerSelected(QuestionItem.AnswerSelectedArgs args)
        {
            await SaveAnswerAsync(args.QuestionId, args.AnswerId);
        }

        private async Task HandleChildAnswerSelected(QuestionItem.AnswerSelectedArgs args)
        {
            await SaveChildAnswerAsync(args.QuestionId, args.AnswerId);
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
                else if (response != null)
                {
                    // Xử lý các trường hợp lỗi cụ thể
                    if (response.IsRateLimited)
                    {
                        // HTTP 429 - Rate limit exceeded
                        Snackbar.Add($"⚠️ {response.Message} (Thử lại sau {response.RetryAfterSeconds} giây)", Severity.Warning, config =>
                        {
                            config.VisibleStateDuration = 5000;
                        });
                        
                        // Lưu answer vào cache để retry sau
                        selectedAnswers[key] = value.ToString();
                        StateHasChanged();
                        
                        // Tự động retry sau khi hết rate limit
                        _ = ScheduleAutoRetryAsync(response.RetryAfterSeconds);
                        
                        return false; // Không thành công nhưng đã lưu vào cache
                    }
                    else if (response.IsUnauthorized)
                    {
                        Snackbar.Add("🔐 Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", Severity.Error);
                        // Có thể redirect về trang login
                        return false;
                    }
                    else
                    {
                        // Các lỗi khác
                        Snackbar.Add($"❌ {response.Message}", Severity.Error);
                        return false;
                    }
                }
                else
                {
                    Snackbar.Add("❌ Không thể lưu đáp án. Vui lòng thử lại.", Severity.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"❌ Lỗi kết nối: {ex.Message}", Severity.Error);
                return false;
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
                else if (response != null)
                {
                    // Xử lý các trường hợp lỗi cụ thể
                    if (response.IsRateLimited)
                    {
                        // HTTP 429 - Rate limit exceeded
                        Snackbar.Add($"⚠️ {response.Message} (Thử lại sau {response.RetryAfterSeconds} giây)", Severity.Warning, config =>
                        {
                            config.VisibleStateDuration = 5000;
                        });
                        
                        // Lưu answer vào cache để retry sau
                        selectedChildAnswers[key] = value.ToString();
                        StateHasChanged();
                        
                        // Tự động retry sau khi hết rate limit
                        _ = ScheduleAutoRetryAsync(response.RetryAfterSeconds);
                        
                        return false; // Không thành công nhưng đã lưu vào cache
                    }
                    else if (response.IsUnauthorized)
                    {
                        Snackbar.Add("🔐 Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", Severity.Error);
                        return false;
                    }
                    else
                    {
                        // Các lỗi khác
                        Snackbar.Add($"❌ {response.Message}", Severity.Error);
                        return false;
                    }
                }
                else
                {
                    Snackbar.Add("❌ Không thể lưu đáp án. Vui lòng thử lại.", Severity.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"❌ Lỗi kết nối: {ex.Message}", Severity.Error);
                return false;
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
                    LoadAnswersFromString(answersString);
                    StateHasChanged(); // Cập nhật UI sau khi load answers
                    
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

        private void LoadAnswersFromString(string answersString)
        {
            if (string.IsNullOrEmpty(answersString))
                return;

            try
            {
                var answerPairs = answersString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var pair in answerPairs)
                {
                    // Loại bỏ dấu ngoặc đơn
                    var cleanPair = pair.Trim('(', ')');
                    
                    // Tách key và value - support cả format cũ (dấu phẩy) và mới (dấu hai chấm)
                    var parts = cleanPair.Split(':');
                    if (parts.Length != 2)
                    {
                        parts = cleanPair.Split(',');
                    }
                    
                    if (parts.Length == 2)
                    {
                        var questionIdStr = parts[0].Trim();
                        var answerIdStr = parts[1].Trim();
                        
                        // Parse question ID
                        if (int.TryParse(questionIdStr, out int questionId))
                        {
                            // Lưu TẤT CẢ câu hỏi, kể cả câu bỏ trống
                            if (!string.IsNullOrEmpty(answerIdStr))
                            {
                                if (answerIdStr == "-")
                                {
                                    // Câu hỏi bỏ trống - lưu với giá trị "-"
                                    var isChildQuestion = IsChildQuestion(questionId);
                                    if (isChildQuestion)
                                    {
                                        selectedChildAnswers[questionId] = "-";
                                    }
                                    else
                                    {
                                        selectedAnswers[questionId] = "-";
                                    }
                                }
                                else if (int.TryParse(answerIdStr, out int answerId))
                                {
                                    // Câu hỏi đã trả lời - lưu với answerId
                                    var isChildQuestion = IsChildQuestion(questionId);
                                    if (isChildQuestion)
                                    {
                                        selectedChildAnswers[questionId] = answerId.ToString();
                                    }
                                    else
                                    {
                                        selectedAnswers[questionId] = answerId.ToString();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error nếu cần
                Console.WriteLine($"Error parsing answers string: {ex.Message}");
            }
        }

        private bool IsChildQuestion(int questionId)
        {
            // Kiểm tra xem questionId có phải là câu hỏi con không
            // Dựa vào mappedQuestions để xác định
            if (mappedQuestions == null)
                return false;

            foreach (var question in mappedQuestions)
            {
                // Kiểm tra câu hỏi con
                if (question.ChildQuestions?.Any() == true)
                {
                    foreach (var childQuestion in question.ChildQuestions)
                    {
                        if (childQuestion.OriginalExamPaperDetailId == questionId)
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

       private async Task OnSubmitExamAsync()
       {
           // Lấy chuỗi thời gian dạng "mm:ss"
           var formattedTime = $"{remainingMinutes:D2}:{remainingSeconds:D2}";
           // Tách phút (phần trước dấu :)
           var minutesOnly = formattedTime.Split(':')[0];
           // Tạo message chỉ với phút
           var timeMessage = $"⏰ Thời gian còn lại: {minutesOnly} phút";

           var dialog = await Dialog.ShowMessageBox("Bạn có chắc chắn muốn nộp bài thi này?", timeMessage, "Nộp bài", "Hủy");
           if (dialog == true)
           {
               try
               {
                   // Timer đã được quản lý bởi ExamTimer component
                   
                   // Lưu đáp án cuối cùng trước khi nộp bài
                   await RefreshAnswersAsync(false);
                   
                   var submitRequest = new SubmitExamRequest
                   {
                       StudentExamSessionId = studentExamSessionId.Value
                   };
                                          
                   var submitResponse = await StudentService.SubmitExamAsync(submitRequest);

                   if (submitResponse?.Success == true)
                   {
                                          // Lưu dữ liệu exam result vào localStorage để trang Result có thể đọc
                   if (submitResponse.Data != null)
                   {
                       var examResultData = new
                       {
                           StudentCode = submitResponse.Data.StudentCode ?? "",
                           ShuffledExamPaperId = submitResponse.Data.ShuffledExamPaperId,
                           Score = submitResponse.Data.Score ?? 0,
                           CorrectAnswers = submitResponse.Data.CorrectAnswers ?? 0,
                           TotalQuestions = submitResponse.Data.TotalQuestions ?? 0,
                           StartTime = submitResponse.Data.StartTime,
                           EndTime = submitResponse.Data.EndTime,
                           StudentAnswersString = submitResponse.Data.StudentAnswersString ?? "",
                           AnswerKey = submitResponse.Data.AnswerKey ?? ""
                       };
                       
                       var resultJson = JsonSerializer.Serialize(examResultData);
                       await JSRuntime.InvokeVoidAsync("localStorage.setItem", $"examResult_{studentExamSessionId.Value}", resultJson);
                       
                       // Lưu studentExamSessionId để trang Result có thể đọc
                       await JSRuntime.InvokeVoidAsync("localStorage.setItem", "currentStudentExamSessionId", studentExamSessionId.Value.ToString());
                       
                       // ===================== BẢO MẬT: Lưu các flag bảo mật =====================
                       // Flag để xác nhận bài thi đã hoàn thành
                       await JSRuntime.InvokeVoidAsync("localStorage.setItem", "examCompletedFlag", "true");
                       
                       // Thời gian nộp bài để kiểm tra thời hạn truy cập
                       await JSRuntime.InvokeVoidAsync("localStorage.setItem", "examSubmitTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                   }
                       
                                               Snackbar.Add("Nộp bài thành công!", Severity.Success);
                        try
                        {
                            await JSRuntime.InvokeVoidAsync("window.location.assign", "/Exam/Result");
                        }
                        catch (Exception ex)
                        {
                            // Fallback nếu JavaScript interop fail
                            Navigation.NavigateTo("/Exam/Result");
                        }
                   }
                   else if (submitResponse != null)
                   {
                       // Xử lý các trường hợp lỗi cụ thể
                       if (submitResponse.IsRateLimited)
                       {
                           // ⚠️ QUAN TRỌNG: HTTP 429 - KHÔNG được chuyển trang kết quả!
                           // Giữ nguyên trang làm bài để sinh viên có thể nộp lại
                           Snackbar.Add($"🚫 {submitResponse.Message} (Thử lại sau {submitResponse.RetryAfterSeconds} giây)", Severity.Warning, config =>
                           {
                               config.VisibleStateDuration = 8000;
                           });
                           
                           // Timer đã được quản lý bởi ExamTimer component
                           
                           // Tự động retry sau khi hết rate limit
                           _ = Task.Run(async () =>
                           {
                               await Task.Delay(submitResponse.RetryAfterSeconds * 1000);
                               await InvokeAsync(async () =>
                               {
                                   Snackbar.Add("🔄 Đang tự động thử nộp bài lại...", Severity.Info);
                                   await OnSubmitExamAsync();
                               });
                           });
                           
                           return; // Không chuyển trang, giữ nguyên trang làm bài
                       }
                       else if (submitResponse.IsUnauthorized)
                       {
                           Snackbar.Add("🔐 Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", Severity.Error);
                           // Có thể redirect về trang login
                           return;
                       }
                       else
                       {
                           // Các lỗi khác
                           Snackbar.Add($"❌ {submitResponse.Message}", Severity.Error);
                       }
                   }
                   else
                   {
                       Snackbar.Add("❌ Có lỗi xảy ra khi nộp bài. Vui lòng thử lại!", Severity.Error);
                   }
               }
               catch (Exception ex)
               {
                   Snackbar.Add("Có lỗi xảy ra khi nộp bài. Vui lòng thử lại!", Severity.Error);
               }
           }
       }



        private async Task InitializeTimerAsync()
        {
            // Timer initialization đã được chuyển vào ExamTimer component
            // Chỉ cần khởi tạo các giá trị mặc định
            if (studentSession?.Duration > 0)
            {
                remainingMinutes = studentSession.Duration + studentSession.ExtraMinutes;
                remainingSeconds = 0;
                examStartTime = DateTime.Now;
                isTimeUp = false;
            }
        }
        


        private async void OnAutoSaveElapsed(object? sender, System.Timers.ElapsedEventArgs e)
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

        // Method để tự động retry sau một khoảng thời gian
        private async Task ScheduleAutoRetryAsync(int delaySeconds)
        {
            await Task.Delay(delaySeconds * 1000);
            await InvokeAsync(async () =>
            {
                await RetryFailedAnswersAsync();
            });
        }



        // Callback methods cho ExamTimer component
        private async Task OnTimerInitialized(frontend_manage.Pages.Exam.Components.ExamTimerData timerData)
        {
            // Cập nhật state từ ExamTimer
            remainingMinutes = (int)timerData.Duration;
            remainingSeconds = 0;
            examStartTime = timerData.StartTime;
            isTimeUp = false;
            StateHasChanged();
        }

        private async Task OnTimeUp()
        {
            // Timer đã hết thời gian
            isTimeUp = true;
            remainingMinutes = 0;
            remainingSeconds = 0;
            StateHasChanged();
            
            // Auto submit exam
            await OnSubmitExamAsync();
        }



        // Method để retry các answers chưa được lưu thành công
        private async Task RetryFailedAnswersAsync()
        {
            if (studentExamSessionId == null || studentSession == null)
                return;

            try
            {
                var failedAnswers = new List<(int questionId, string answerId, bool isChildQuestion)>();
                
                // Kiểm tra các answers đã chọn nhưng chưa được lưu thành công
                foreach (var answer in selectedAnswers)
                {
                    if (!string.IsNullOrEmpty(answer.Value))
                    {
                        failedAnswers.Add((answer.Key, answer.Value, false));
                    }
                }
                
                foreach (var answer in selectedChildAnswers)
                {
                    if (!string.IsNullOrEmpty(answer.Value))
                    {
                        failedAnswers.Add((answer.Key, answer.Value, true));
                    }
                }
                
                if (failedAnswers.Any())
                {
                    Snackbar.Add($"🔄 Đang tự động lưu lại {failedAnswers.Count} đáp án...", Severity.Info);
                    
                    foreach (var (questionId, answerId, isChildQuestion) in failedAnswers)
                    {
                        if (int.TryParse(answerId, out int answerIdInt))
                        {
                            if (isChildQuestion)
                            {
                                await SaveChildAnswerAsync(questionId, answerIdInt);
                            }
                            else
                            {
                                await SaveAnswerAsync(questionId, answerIdInt);
                            }
                            
                            // Delay nhỏ giữa các lần gọi để tránh rate limit
                            await Task.Delay(500);
                        }
                    }
                    
                    Snackbar.Add("✅ Đã tự động lưu lại tất cả đáp án", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"❌ Lỗi khi tự động lưu lại đáp án: {ex.Message}", Severity.Error);
            }
        }



        public void Dispose()
        {
            autoSaveTimer?.Stop();
            autoSaveTimer?.Dispose();
        }



       
        


        


       
       
       
       
       
       
       
       
       
       
       

        

        
        
    }
}