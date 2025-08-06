using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Text.RegularExpressions;
using frontend_manage.DTOs;

namespace frontend_manage.Pages.Exam
{
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

        protected override async Task OnInitializedAsync()
        {
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
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
        }

        private async Task OnAnswerSelected(int questionId, string answer, int order)
        {
            Console.WriteLine($"🔄 TEST: OnAnswerSelected được gọi với questionId={questionId}, answer={answer}, order={order}");
            
            // Cập nhật selected answer trước
            selectedAnswers[questionId] = $"q{questionId}_{answer switch { "A" => "1", "B" => "2", "C" => "3", "D" => "4", _ => "1" }}";
            
            // Gọi API save answer
            await SaveAnswerAsync(order, null, answer);
            
            StateHasChanged();
        }

        private async Task OnChildAnswerSelected(int questionId, string answer, int parentOrder, int childOrder)
        {
            Console.WriteLine($"🔄 TEST: OnChildAnswerSelected được gọi với questionId={questionId}, answer={answer}, parentOrder={parentOrder}, childOrder={childOrder}");
            
            // Cập nhật selected child answer trước
            selectedChildAnswers[questionId] = $"cq{questionId}_{answer switch { "A" => "1", "B" => "2", "C" => "3", "D" => "4", _ => "1" }}";
            
            // Gọi API save answer với subindex
            await SaveAnswerAsync(parentOrder, childOrder, answer);
            
            StateHasChanged();
        }

        private async Task SaveAnswerAsync(int index, int? subIndex, string answer)
        {
            if (studentExamSessionId == null || studentSession == null)
            {
                Console.WriteLine("❌ StudentExamSessionId hoặc studentSession là null");
                return;
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
                }
                else
                {
                    Console.WriteLine($"❌ Lỗi khi lưu đáp án: {response?.Message ?? "Không xác định"}");
                    Snackbar.Add($"Lỗi khi lưu đáp án: {response?.Message ?? "Không xác định"}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception khi lưu đáp án: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                Snackbar.Add($"Lỗi khi lưu đáp án: {ex.Message}", Severity.Error);
            }
        }

        private string GetSelectedAnswer(int questionId)
        {
            return selectedAnswers.TryGetValue(questionId, out var answer) ? answer : "";
        }

        private string GetSelectedChildAnswer(int questionId)
        {
            return selectedChildAnswers.TryGetValue(questionId, out var answer) ? answer : "";
        }

        private async Task OnSubmitExamAsync()
        {
            var dialog = await Dialog.ShowMessageBox("Xác nhận", "Bạn có chắc chắn muốn nộp bài thi này?", "Nộp bài", "Hủy");
            if (dialog == true)
            {
                // TODO: Implement submit exam logic
                Snackbar.Add("Đã nộp bài thi thành công!", Severity.Success);
                
                // Chuyển sang trang Result
                Navigation.NavigateTo($"/Exam/Result?studentExamSessionId={studentExamSessionId}");
            }
        }

        private async Task ScrollToQuestionAsync(int questionNumber)
        {
            await JSRuntime.InvokeVoidAsync("scrollToElement", $"question-{questionNumber}");
        }

        private int GetAnsweredQuestionsCount()
        {
            return selectedAnswers.Count + selectedChildAnswers.Count;
        }

        private int GetTotalQuestionsCount()
        {
            if (shuffledExam?.Details == null) return 0;
            
            int total = shuffledExam.Details.Count;
            foreach (var detail in shuffledExam.Details)
            {
                if (detail.ChildQuestions != null)
                {
                    total += detail.ChildQuestions.Count;
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