using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace frontend_manage.test
{
    public partial class Exam : ComponentBase
    {
        [Parameter]
        [SupplyParameterFromQuery]
        public int? studentExamSessionId { get; set; }

        private StartExamResponseDto? startExamResponse;
        private ShuffledExamPaperDto? shuffledExam;
        private string? errorMessage;
        
        // Timer variables (fake)
        private int remainingMinutes = 90;
        private int remainingSeconds = 0;
        private bool isTimeUp = false;
        private DateTime? lastSaveTime = DateTime.Now.AddMinutes(-5);

        protected override async Task OnInitializedAsync()
        {
            // Tạo dữ liệu giả cho test
            await CreateFakeData();
        }

        private async Task CreateFakeData()
        {
            // Chỉ tạo dữ liệu đề thi giả
            shuffledExam = new ShuffledExamPaperDto
            {
                Title = "ĐỀ THI CUỐI KỲ MÔN LẬP TRÌNH WEB",
                ShuffledExamPaperCore = "WEB001_2024",
                SubjectName = "Lập trình Web",
                SubjectCode = "WEB001",
                Details = new List<ShuffledExamPaperDetailDto>
                {
                    new ShuffledExamPaperDetailDto
                    {
                        ShuffledExamPaperDetailId = 1,
                        Order = 1,
                        QuestionContent = "HTML là viết tắt của từ gì?",
                        Answer1 = "HyperText Markup Language",
                        Answer2 = "Home Tool Markup Language", 
                        Answer3 = "Hyperlinks and Text Markup Language",
                        Answer4 = "HyperText Making Language"
                    },
                    new ShuffledExamPaperDetailDto
                    {
                        ShuffledExamPaperDetailId = 2,
                        Order = 2,
                        QuestionContent = "CSS được sử dụng để làm gì?",
                        Answer1 = "Tạo cấu trúc trang web",
                        Answer2 = "Tạo giao diện và định dạng trang web",
                        Answer3 = "Tạo chức năng động cho trang web", 
                        Answer4 = "Lưu trữ dữ liệu"
                    },
                    new ShuffledExamPaperDetailDto
                    {
                        ShuffledExamPaperDetailId = 3,
                        Order = 3,
                        QuestionContent = "<audio>sample-audio.mp3</audio> Nghe đoạn audio sau và chọn đáp án đúng:",
                        Answer1 = "JavaScript",
                        Answer2 = "Python",
                        Answer3 = "C#",
                        Answer4 = "Java"
                    }
                }
            };
        }

        // Fake methods để giao diện không bị lỗi
        private string GetSelectedAnswer(int questionId) => "";
        private string GetSelectedChildAnswer(int questionId) => "";
        private async Task OnAnswerSelected(int questionId, string answer, int order) { }
        private async Task OnChildAnswerSelected(int questionId, string answer, int parentOrder, int childOrder) { }
        private async Task ScrollToQuestionAsync(string questionIdentifier) { }
        private string GetFormattedTime() => $"{remainingMinutes:D2}:{remainingSeconds:D2}";
        private async Task OnSubmitExamAsync() { }
        private int GetAnsweredQuestionsCount() => 0;
        private int GetTotalQuestionsCount() => shuffledExam?.Details?.Count ?? 0;
        private string GetQuestionCountDetails() => $"{GetTotalQuestionsCount()} câu đơn";

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
                var audioId = $"audio_{audioPath.GetHashCode().ToString().Replace("-", "n")}";
                
                // Thay thế thẻ audio bằng button đơn giản
                var audioButton = $@"
                <div class=""audio-player mb-3"">
                    <audio id=""{audioId}"" style=""display: none;"">
                        <source src=""/test-audio/{audioPath}"" type=""audio/mpeg"">
                    </audio>
                    <button class=""mud-button-root mud-button mud-button-filled mud-button-filled-primary mud-button-filled-size-medium mud-ripple"" 
                            onclick=""console.log('Audio button clicked')"">
                        <span class=""mud-button-label"">
                            🔊 Phát audio (Test Mode)
                        </span>
                    </button>
                </div>";
                
                return Regex.Replace(content, audioPattern, audioButton);
            }
            
            return content;
        }

        public void Dispose()
        {
            // Cleanup nếu cần
        }
    }
}