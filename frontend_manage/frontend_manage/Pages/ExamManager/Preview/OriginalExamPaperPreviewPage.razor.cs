using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs;
using frontend_manage.Services.ExamManager;
using frontend_manage.Services;

namespace frontend_manage.Pages.ExamManager.Preview
{
    public partial class OriginalExamPaperPreviewPage : ComponentBase
    {
        [Parameter]
        [SupplyParameterFromQuery]
        public string? core { get; set; }

        [Inject]
        private ExamManagerService ExamManagerService { get; set; } = null!;

        [Inject]
        private IKaTeXService KaTeX { get; set; } = null!;

        private OriginalExamPaperDto? Exam;
        private bool isLoading = true;
        private bool isUpdating = false;

        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrWhiteSpace(core))
            {
                isLoading = true;
                try
                {
                    Exam = await ExamManagerService.GetOriginalExamWithDetailsAsync(core);
                }
                catch (Exception ex)
                {
                    // Handle error
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

        private List<OriginalExamPaperDetailDto> GetFlatQuestions()
        {
            if (Exam?.Details == null || Exam.Details.Count == 0)
                return new List<OriginalExamPaperDetailDto>();

            var flatQuestions = new List<OriginalExamPaperDetailDto>();
            var parentQuestions = Exam.Details.Where(d => d.ParentQuestionId == null).OrderBy(d => d.Order).ToList();
            
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
            if (Exam?.Details == null || Exam.Details.Count == 0)
                return 0;

            int count = 0;
            foreach (var q in Exam.Details.Where(d => d.ParentQuestionId == null))
            {
                var isParentQuestion = q.ChildQuestions != null && q.ChildQuestions.Count > 0;
                
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
            if (Exam == null || string.IsNullOrWhiteSpace(Exam.OriginalExamPaperCore))
                return;

            var newValue = e.Value != null && (bool)e.Value;
            var oldValue = Exam.AllowViewMaterials;
            
            // Optimistic update
            Exam.AllowViewMaterials = newValue;
            isUpdating = true;
            StateHasChanged();

            try
            {
                await ExamManagerService.UpdateOriginalExamAllowViewMaterialsAsync(Exam.OriginalExamPaperCore, newValue);
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

