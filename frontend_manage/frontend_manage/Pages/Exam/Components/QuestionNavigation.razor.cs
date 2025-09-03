using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using frontend_manage.DTOs;

namespace frontend_manage.Pages.Exam.Components
{
    public partial class QuestionNavigation : BaseComponent
    {
        [Parameter] public List<QuestionStructureDto>? Questions { get; set; }
        [Parameter] public Dictionary<int, string> SelectedAnswers { get; set; } = new();
        [Parameter] public Dictionary<int, string> SelectedChildAnswers { get; set; } = new();

        
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        private bool IsQuestionAnswered(int originalExamPaperDetailId)
        {
            return !string.IsNullOrEmpty(GetSelectedAnswer(originalExamPaperDetailId));
        }

        private bool IsChildQuestionAnswered(int originalExamPaperDetailId)
        {
            return !string.IsNullOrEmpty(GetSelectedChildAnswer(originalExamPaperDetailId));
        }

        private string GetSelectedAnswer(int originalExamPaperDetailId)
        {
            SelectedAnswers.TryGetValue(originalExamPaperDetailId, out string? selectedValue);
            return selectedValue ?? string.Empty;
        }

        private string GetSelectedChildAnswer(int originalExamPaperDetailId)
        {
            SelectedChildAnswers.TryGetValue(originalExamPaperDetailId, out string? selectedValue);
            return selectedValue ?? string.Empty;
        }

        private int GetTotalQuestionsCount()
        {
            if (Questions == null) return 0;

            int total = 0;
            foreach (var q in Questions)
            {
                if (q.ChildQuestions != null && q.ChildQuestions.Any())
                {
                    total += q.ChildQuestions.Count;
                }
                else
                {
                    total++;
                }
            }
            return total;
        }

        private int GetAnsweredQuestionsCount()
        {
            if (Questions == null) return 0;

            int answered = 0;
            foreach (var q in Questions)
            {
                if (q.ChildQuestions != null && q.ChildQuestions.Any())
                {
                    foreach (var childQ in q.ChildQuestions)
                    {
                        if (IsChildQuestionAnswered(childQ.OriginalExamPaperDetailId))
                        {
                            answered++;
                        }
                    }
                }
                else
                {
                    if (IsQuestionAnswered(q.OriginalExamPaperDetailId))
                    {
                        answered++;
                    }
                }
            }
            return answered;
        }

        // Method để scroll đến câu hỏi cụ thể
        public async Task ScrollToQuestionAsync(string questionIdentifier)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("scrollToElement", $"question-{questionIdentifier}");
            }
            catch (Exception ex)
            {
                // Log error nếu cần
                Console.WriteLine($"Error scrolling to question: {ex.Message}");
            }
        }


    }
}