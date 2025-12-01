using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs;
using frontend_manage.DTOs.Mapp;
using frontend_manage.Services.ExamManager;
using frontend_manage.Services;

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
            if (firstRender)
            {
                await KaTeX.RenderAsync(".shuffled-preview-page");
            }
        }

        private string RenderHtml(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;
            
            // Loại bỏ các ký tự {<number>} khỏi nội dung
            return System.Text.RegularExpressions.Regex.Replace(
                content, 
                @"\{<\d+>\}", 
                string.Empty
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

            var flatQuestions = new List<QuestionStructureDto>();
            var parentQuestions = Exam.QuestionStructures.Where(d => d.ParentQuestionId == null).OrderBy(d => d.Order).ToList();
            
            foreach (var parentQ in parentQuestions)
            {
                // Nếu là câu hỏi cha (có child questions)
                if (parentQ.ChildQuestions != null && parentQ.ChildQuestions.Count > 0)
                {
                    flatQuestions.Add(parentQ); // Thêm câu hỏi cha
                    // Thêm các câu hỏi con
                    foreach (var child in parentQ.ChildQuestions.OrderBy(c => c.Order))
                    {
                        flatQuestions.Add(child);
                    }
                }
                // Nếu là câu hỏi độc lập (không có child)
                else
                {
                    flatQuestions.Add(parentQ);
                }
            }

            return flatQuestions;
        }

        private int GetTotalQuestionsCount()
        {
            if (Exam?.QuestionStructures == null || Exam.QuestionStructures.Count == 0)
                return 0;

            int count = 0;
            foreach (var q in Exam.QuestionStructures)
            {
                var isParentQuestion = q.ParentQuestionId == null && q.ChildQuestions != null && q.ChildQuestions.Count > 0;
                
                if (isParentQuestion)
                {
                    // Câu hỏi cha: chỉ đếm các câu hỏi con
                    if (q.ChildQuestions != null && q.ChildQuestions.Count > 0)
                    {
                        count += q.ChildQuestions.Count;
                    }
                }
                else
                {
                    // Câu hỏi độc lập: đếm chính nó
                    count++;
                }
            }
            return count;
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

