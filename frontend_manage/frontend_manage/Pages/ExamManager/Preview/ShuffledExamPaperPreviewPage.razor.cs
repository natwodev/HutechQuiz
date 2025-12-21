using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using frontend_manage.DTOs;
using frontend_manage.DTOs.Mapp;
using frontend_manage.Services.ExamManager;
using frontend_manage.Services;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
namespace frontend_manage.Pages.ExamManager.Preview
{
    public partial class ShuffledExamPaperPreviewPage : ComponentBase
    {
        [Parameter]
        [SupplyParameterFromQuery]
        public string? core { get; set; }

        [Inject]
        private ExamManagerService ExamManagerService { get; set; } = null!;

        [Inject]
        private IKaTeXService KaTeX { get; set; } = null!;

        [Inject]
        private IExamRenderingService ExamRenderingService { get; set; } = null!;

        [Inject]
        private IJSRuntime JS { get; set; } = null!;

        // Dùng HttpClient để lấy BaseAddress của backend (đã cấu hình qua ApiBaseUrl)
        [Inject]
        private HttpClient HttpClient { get; set; } = null!;
        [Inject]
        private IConfiguration Configuration { get; set; } = null!;

        private ShuffledExamPaperDto? Exam;
        private OriginalExamPaperDto? OriginalExam;
        private bool isLoading = true;
        private bool isUpdating = false;
        private Dictionary<int, bool>? _correctAnswersMap;

        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrWhiteSpace(core))
            {
                isLoading = true;
                try
                {
                    Exam = await ExamManagerService.GetShuffledExamWithDetailsAsync(core);
                    
                    // Sử dụng OriginalExamPaper từ response và map bằng QuestionMapping
                    if (Exam != null && Exam.OriginalExamPaper != null)
                    {
                        OriginalExam = Exam.OriginalExamPaper;
                        
                        if (OriginalExam.Details != null)
                        {
                            // Sử dụng QuestionMapping để map từ OriginalExamPaperDetailDto sang QuestionStructureDto
                            Exam.QuestionStructures = QuestionMapping.MapToQuestionStructureList(OriginalExam.Details);
                            
                            // Tạo map để xác định đáp án đúng
                            BuildCorrectAnswersMap();
                            
                            StateHasChanged(); // Cập nhật UI sau khi map
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle error
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi tải đề thi: {ex.Message}");
                }
                finally
                {
                    isLoading = false;
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Render KaTeX mỗi lần component render để đảm bảo các elements mới được xử lý
            try
        {
            if (firstRender)
            {
                    // Sử dụng RenderWithRetryAsync cho first render để đảm bảo KaTeX đã sẵn sàng
                    await KaTeX.RenderWithRetryAsync(".katex-content", maxRetries: 3, delayMs: 100);
                }
                else
                {
                    // Re-render khi content thay đổi
                    await KaTeX.RenderAsync(".katex-content");
                }
            }
            catch (Exception ex)
            {
                // Log error nhưng không throw để không làm gián đoạn UI
                System.Diagnostics.Debug.WriteLine($"Error rendering KaTeX in shuffled preview: {ex.Message}");
            }

            // Vẽ đường nối cho matching questions trong preview mode
            if (Exam?.QuestionStructures != null && Exam.QuestionStructures.Any(q => IsMatchingParent(q)))
            {
                // Gọi lại sau khi render xong để đảm bảo DOM đã sẵn sàng
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1500); // Đợi DOM và KaTeX render xong
                    await InvokeAsync(async () =>
                    {
                        await DrawMatchingLinesAsync();
                        StateHasChanged();
                    });
                });
            }
        }

        private async Task DrawMatchingLinesAsync()
        {
            if (Exam?.QuestionStructures == null)
                return;

            try
            {
                // Đợi để DOM và KaTeX đã render xong hoàn toàn (giống MatchingQuestion component)
                await Task.Delay(300);

                foreach (var q in Exam.QuestionStructures)
                {
                    if (IsMatchingParent(q))
                    {
                        var correctPairs = GetMatchingCorrectPairs(q);
                        if (correctPairs != null && correctPairs.Count > 0)
                        {
                            var containerId = $"match-{q.OriginalExamPaperDetailId}";
                            
                            // Clear các pairs cũ trước (giống MatchingQuestion component)
                            
                            foreach (var pair in correctPairs)
                            {
                                var leftId = pair.Key;
                                var rightId = pair.Value;
                                
                                // Vẽ đường nối cho mỗi cặp đúng (giống MatchingQuestion component)
                                try
                                {
                                    await DrawLineAsync(containerId, leftId, rightId);
                                    System.Diagnostics.Debug.WriteLine($"Drew line: left-{leftId} -> right-{rightId} in container {containerId}");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error drawing preview line from left-{leftId} to right-{rightId}: {ex.Message}");
            }
                            }
                            
                            // Re-draw tất cả các đường nối sau khi đã vẽ xong (giống MatchingQuestion component)
                            await Task.Delay(100);
                            try
                            {
                                await JS.InvokeVoidAsync("matchingHelpers.redrawLeaderLines", containerId);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error redrawing leader lines for {containerId}: {ex.Message}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"No correct pairs found for matching question {q.OriginalExamPaperDetailId}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error drawing matching lines: {ex.Message}");
            }
        }

        private async Task DrawLineAsync(string containerId, int leftId, int rightId)
        {
            // Use LeaderLine to draw dynamic connection (giống MatchingQuestion component)
            await JS.InvokeVoidAsync("matchingHelpers.addLeaderLine", containerId, $"left-{leftId}", $"right-{rightId}");
        }

        private string RenderHtml(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            // Sử dụng ExamRenderingService để normalize và render content
            if (Exam == null || string.IsNullOrEmpty(Exam.ShuffledExamPaperCore))
                return content;

            var folderName = Exam.ShuffledExamPaperCore.Split('_')[0];
            return ExamRenderingService.NormalizeAndRenderContent(
                content,
                shuffledExamPaperCore: Exam.ShuffledExamPaperCore,
                originalExamPaperCore: null
            );
        }

        private string NormalizeMatchingContent(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            // Loại bỏ số và chữ cái đầu nếu có (ví dụ: "1. Iodine" → "Iodine", "a. Starch indicator" → "Starch indicator")
            // Vì frontend sẽ tự động thêm số và chữ cái khi render
            var cleaned = content.Trim();
            
            // Loại bỏ số đầu: "1. ", "2. ", "10. ", etc.
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^\d+\.\s*", string.Empty);
            
            // Loại bỏ chữ cái đầu: "a. ", "b. ", "A. ", etc.
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^[a-z]\.\s*", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Sử dụng ExamRenderingService để normalize và render content
            if (Exam == null || string.IsNullOrEmpty(Exam.ShuffledExamPaperCore))
                return cleaned;

            return ExamRenderingService.NormalizeAndRenderContent(
                cleaned,
                shuffledExamPaperCore: Exam.ShuffledExamPaperCore,
                originalExamPaperCore: null
            );
        }
        
        private char GetLetter(int order) => (char)('A' + Math.Max(0, order - 1));

        private void BuildCorrectAnswersMap()
        {
            if (OriginalExam?.Details == null)
                return;

            _correctAnswersMap = new Dictionary<int, bool>();
            
            // Duyệt qua tất cả các câu hỏi trong OriginalExam (bao gồm cả câu hỏi con)
            foreach (var detail in OriginalExam.Details)
            {
                // Xử lý answers của câu hỏi cha hoặc câu hỏi độc lập
                if (detail.Answers != null)
                {
                    foreach (var answer in detail.Answers)
                    {
                        // Map AnswerId với IsCorrect
                        _correctAnswersMap[answer.AnswerId] = answer.IsCorrect;
                    }
                }
                
                // Xử lý answers của câu hỏi con
                if (detail.ChildQuestions != null)
                {
                    foreach (var childQuestion in detail.ChildQuestions)
                    {
                        if (childQuestion.Answers != null)
                        {
                            foreach (var answer in childQuestion.Answers)
                            {
                                // Map AnswerId với IsCorrect cho câu hỏi con
                                _correctAnswersMap[answer.AnswerId] = answer.IsCorrect;
                            }
                        }
                    }
                }
            }
        }

        private bool IsCorrectAnswer(int answerId)
        {
            if (_correctAnswersMap == null)
                return false;
            
            return _correctAnswersMap.TryGetValue(answerId, out var isCorrect) && isCorrect;
        }

        private List<QuestionStructureDto> GetFlatQuestions()
        {
            if (Exam?.QuestionStructures == null || Exam.QuestionStructures.Count == 0)
                return new List<QuestionStructureDto>();

            // Sử dụng ExamRenderingService để flatten questions
            var flatQuestions = ExamRenderingService.GetFlatQuestions(
                Exam.QuestionStructures,
                q => q.ParentQuestionId,
                q => q.ChildQuestions,
                q => q.Order
            );
            
            // Loại bỏ child questions của matching parent (chúng đã được render trong MatchingQuestion)
            var filteredQuestions = new List<QuestionStructureDto>();
            foreach (var q in flatQuestions)
            {
                // Nếu là child question của matching parent, bỏ qua
                if (q.ParentQuestionId.HasValue)
                {
                    // Tìm parent question trong Exam.QuestionStructures
                    var parent = Exam.QuestionStructures.FirstOrDefault(p => p.OriginalExamPaperDetailId == q.ParentQuestionId.Value);
                    if (parent != null && IsMatchingParent(parent))
                    {
                        continue; // Bỏ qua child question của matching parent
                    }
                }
                
                filteredQuestions.Add(q);
            }

            return filteredQuestions;
        }

        private int GetTotalQuestionsCount()
        {
            if (Exam?.QuestionStructures == null || Exam.QuestionStructures.Count == 0)
                return 0;

            // Sử dụng ExamRenderingService để đếm tổng số câu hỏi
            return ExamRenderingService.GetTotalQuestionsCount(
                Exam.QuestionStructures,
                q => q.ParentQuestionId,
                q => q.ChildQuestions
            );
        }

        /// <summary>
        /// Kiểm tra xem đây có phải là matching question dạng parent-child không
        /// </summary>
        private bool IsMatchingParent(QuestionStructureDto q)
        {
            if (q.ChildQuestions == null || q.ChildQuestions.Count == 0)
                return false;

            // Parent question không nên có answers (chỉ có stem)
            if (q.Answers != null && q.Answers.Count > 0)
                return false;

            // Kiểm tra xem tất cả child questions có cùng số lượng answers không
            var firstChild = q.ChildQuestions.First();
            if (firstChild.Answers == null || firstChild.Answers.Count == 0)
                return false;

            var answerCount = firstChild.Answers.Count;
            
            // Tất cả child questions phải có cùng số lượng answers
            if (q.ChildQuestions.Any(child => child.Answers == null || child.Answers.Count != answerCount))
                return false;

            // Kiểm tra thêm: parent question có stem chứa từ khóa về matching không
            var stem = q.QuestionContent ?? string.Empty;
            var hasMatchingKeywords = stem.Contains("Nối cột", StringComparison.OrdinalIgnoreCase) ||
                                     stem.Contains("nối", StringComparison.OrdinalIgnoreCase) ||
                                     stem.Contains("match", StringComparison.OrdinalIgnoreCase);

            // Nếu có từ khóa matching hoặc pattern đặc biệt (số lượng child >= 2 và số lượng answers >= 2)
            if (hasMatchingKeywords || (q.ChildQuestions.Count >= 2 && answerCount >= 2))
            {
                // Đảm bảo tất cả child questions đều là MCQ (có answers)
                return q.ChildQuestions.All(child => 
                    child.Answers != null && 
                    child.Answers.Count > 0 &&
                    child.Answers.Count == answerCount);
            }

            return false;
        }

        /// <summary>
        /// Lấy left items cho matching question (từ child questions)
        /// </summary>
        private List<AnswerStructureDto> GetMatchingLeftItems(QuestionStructureDto parent)
            {
            var leftItems = new List<AnswerStructureDto>();
            
            if (parent.ChildQuestions == null)
                return leftItems;

            foreach (var child in parent.ChildQuestions.OrderBy(c => c.Order))
            {
                // Tạo AnswerStructureDto từ child question stem
                leftItems.Add(new AnswerStructureDto
                {
                    AnswerId = child.OriginalExamPaperDetailId, // Dùng question ID làm AnswerId
                    AnswerContent = child.QuestionContent ?? string.Empty,
                    Order = child.Order,
                    OriginalExamPaperDetailId = child.OriginalExamPaperDetailId
                });
            }

            return leftItems;
        }

        /// <summary>
        /// Lấy right items cho matching question (từ answers của child đầu tiên)
        /// Giống với OriginalExamPaperPreviewPage: trả về TẤT CẢ answers
        /// </summary>
        private List<AnswerStructureDto> GetMatchingRightItems(QuestionStructureDto parent)
        {
            if (parent.ChildQuestions == null || parent.ChildQuestions.Count == 0)
                return new List<AnswerStructureDto>();

            var firstChild = parent.ChildQuestions.OrderBy(c => c.Order).First();
            if (firstChild.Answers == null)
                return new List<AnswerStructureDto>();

            // Trả về answers từ child đầu tiên (tất cả child đều có cùng answers)
            // Giống với OriginalExamPaperPreviewPage: trả về TẤT CẢ answers
            return firstChild.Answers
                .OrderBy(a => a.Order)
                .Select(a => new AnswerStructureDto
                {
                    AnswerId = a.AnswerId,
                    AnswerContent = a.AnswerContent,
                    Order = a.Order,
                    OriginalExamPaperDetailId = a.OriginalExamPaperDetailId
                })
                .ToList();
        }

        /// <summary>
        /// Lấy đáp án đúng cho matching question (Dictionary<leftQuestionId, rightAnswerId>)
        /// Logic giống với OriginalExamPaperPreviewPage
        /// </summary>
        private Dictionary<int, int> GetMatchingCorrectPairs(QuestionStructureDto parent)
        {
            var correctPairs = new Dictionary<int, int>();
            
            if (parent.ChildQuestions == null || parent.ChildQuestions.Count == 0)
                return correctPairs;

            var firstChild = parent.ChildQuestions.OrderBy(c => c.Order).First();
            if (firstChild.Answers == null || firstChild.Answers.Count == 0)
                return correctPairs;

            // Lấy danh sách answers từ child đầu tiên (tất cả child đều có cùng answers)
            // Giống với OriginalExamPaperPreviewPage: allAnswers là TẤT CẢ answers
            var allAnswers = firstChild.Answers.OrderBy(a => a.Order).ToList();
            
            // Với matching questions, answers được chia thành 2 phần: nửa đầu là Column A, nửa sau là Column B
            var leftCount = allAnswers.Count / 2;
            var rightAnswers = allAnswers.Skip(leftCount).ToList();

            // Duyệt qua từng child question để lấy đáp án đúng
            // Với matching questions, đáp án đúng được xác định từ OriginalExamPaper
            if (OriginalExam?.Details != null)
            {
                foreach (var child in parent.ChildQuestions.OrderBy(c => c.Order))
                {
                    // Tìm child question tương ứng trong OriginalExamPaper để lấy CorrectAnswerIndex
                    var originalChild = OriginalExam.Details.FirstOrDefault(d => d.OriginalExamPaperDetailId == child.OriginalExamPaperDetailId);
                    if (originalChild != null && originalChild.CorrectAnswerIndex.HasValue && originalChild.CorrectAnswerIndex.Value > 0)
                    {
                        // CorrectAnswerIndex là 1-based và chỉ số trong tổng số answers (bao gồm cả Column A và Column B)
                        // Với matching questions, CorrectAnswerIndex trỏ đến answer trong Column B
                        // Cần trừ đi số lượng items trong Column A để lấy index trong Column B
                        var answerIndex = originalChild.CorrectAnswerIndex.Value - 1 - leftCount;
                        
                        // AnswerIndex trong matching question là index trong right answers (cột B)
                        if (answerIndex >= 0 && answerIndex < rightAnswers.Count)
                        {
                            var correctAnswer = rightAnswers[answerIndex];
                            // leftQuestionId = child.OriginalExamPaperDetailId, rightAnswerId = correctAnswer.AnswerId
                            correctPairs[child.OriginalExamPaperDetailId] = correctAnswer.AnswerId;
                            System.Diagnostics.Debug.WriteLine($"Matching pair: left-{child.OriginalExamPaperDetailId} -> right-{correctAnswer.AnswerId} (answerIndex={answerIndex}, leftCount={leftCount}, CorrectAnswerIndex={originalChild.CorrectAnswerIndex.Value})");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Invalid answerIndex: {answerIndex} (leftCount={leftCount}, CorrectAnswerIndex={originalChild.CorrectAnswerIndex.Value}, rightAnswers.Count={rightAnswers.Count})");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Could not find originalChild for child.OriginalExamPaperDetailId={child.OriginalExamPaperDetailId}");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"OriginalExam?.Details is null");
            }

            return correctPairs;
        }

        private int? GetQuestionNumber(QuestionStructureDto question, int indexInFlatList)
                {
            if (Exam?.QuestionStructures == null || Exam.QuestionStructures.Count == 0)
                return null;
            
            // Đếm số thứ tự dựa trên thứ tự gốc trong Exam.QuestionStructures (theo Order)
            // Sắp xếp tất cả questions theo Order
            var allQuestions = Exam.QuestionStructures.OrderBy(q => q.Order).ToList();
            
            int number = 1;
            foreach (var q in allQuestions)
            {
                // Nếu đã đến câu hỏi hiện tại, dừng lại
                if (q.OriginalExamPaperDetailId == question.OriginalExamPaperDetailId)
                {
                    // Nếu là matching parent, trả về số đã đếm
                    if (IsMatchingParent(question))
                    {
                        return number;
                    }
                    // Nếu là parent question thông thường, không có số
                    if (question.ParentQuestionId == null && question.ChildQuestions != null && question.ChildQuestions.Count > 0)
                    {
                        return null;
                    }
                    // Câu hỏi độc lập: trả về số đã đếm
                    return number;
                }
                
                // Đếm các câu hỏi trước câu hỏi hiện tại
                var isMatching = IsMatchingParent(q);
                var isNormalParent = q.ParentQuestionId == null && q.ChildQuestions != null && q.ChildQuestions.Count > 0 && !isMatching;
                
                if (isMatching)
                    {
                    // Matching question: đếm như một câu hỏi độc lập
                    number++;
                }
                else if (isNormalParent)
                {
                    // Parent question thông thường: không đếm parent, chỉ đếm child questions
                    if (q.ChildQuestions != null)
                    {
                        number += q.ChildQuestions.Count;
                    }
                }
                else
                {
                    // Câu hỏi độc lập: đếm
                    number++;
                }
            }
            
            // Nếu không tìm thấy trong allQuestions, trả về null
            return null;
        }

        private async Task HandleToggleAllowViewMaterials(ChangeEventArgs e)
        {
            if (Exam == null || string.IsNullOrWhiteSpace(Exam.ShuffledExamPaperCore))
                return;

            var newValue = e.Value != null && (bool)e.Value;
            var oldValue = Exam.AllowViewMaterials;
            
            // Optimistic update
            Exam.AllowViewMaterials = newValue;
            isUpdating = true;
            StateHasChanged();

            try
            {
                await ExamManagerService.UpdateShuffledExamAllowViewMaterialsAsync(Exam.ShuffledExamPaperCore, newValue);
            }
            catch (Exception ex)
            {
                // Revert on error
                Exam.AllowViewMaterials = oldValue;
                System.Diagnostics.Debug.WriteLine($"Lỗi khi cập nhật AllowViewMaterials: {ex.Message}");
                // Có thể hiển thị thông báo lỗi cho người dùng ở đây
            }
            finally
            {
                isUpdating = false;
                StateHasChanged();
            }
        }
    }
}


