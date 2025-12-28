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
                            // Nếu đã có QuestionStructures từ backend (đã hoán vị), hãy bổ sung nội dung từ OriginalExam.Details
                            if (Exam.QuestionStructures != null && Exam.QuestionStructures.Count > 0)
                            {
                                QuestionMapping.EnrichQuestionStructuresWithContent(Exam.QuestionStructures, OriginalExam.Details);
                            }
                            else
                            {
                                // Fallback nếu backend không trả về QuestionStructures
                                Exam.QuestionStructures = QuestionMapping.MapToQuestionStructureList(OriginalExam.Details);
                            }
                            
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

                // Thay thế audio tags bằng custom controls
                await JS.InvokeVoidAsync("replaceAudioWithCustomControls");
            }
            catch (Exception ex)
            {
                // Log error nhưng không throw để không làm gián đoạn UI
                System.Diagnostics.Debug.WriteLine($"Error rendering KaTeX in shuffled preview: {ex.Message}");
            }
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
            return ExamRenderingService.GetFlatQuestions(
                Exam.QuestionStructures,
                q => q.ParentQuestionId,
                q => q.ChildQuestions,
                q => q.Order
            );
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
                    // Nếu là parent question, không có số
                    if (question.ParentQuestionId == null && question.ChildQuestions != null && question.ChildQuestions.Count > 0)
                    {
                        return null;
                    }
                    // Câu hỏi độc lập: trả về số đã đếm
                    return number;
                }
                
                // Đếm các câu hỏi trước câu hỏi hiện tại
                var isNormalParent = q.ParentQuestionId == null && q.ChildQuestions != null && q.ChildQuestions.Count > 0;
                
                if (isNormalParent)
                {
                    // Parent question: không đếm parent, chỉ đếm child questions
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
