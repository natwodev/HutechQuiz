using frontend_manage.DTOs;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace frontend_manage.Pages.Exam
{
    public partial class Exam : ComponentBase
    {
        [Inject] private ExamMockService Mock { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        protected List<QuestionStructureDto> FlatQuestions { get; set; } = new();
        protected Dictionary<int, object?> AnsweredMap { get; set; } = new();
        protected Dictionary<int, object?> AnsweredMapDerived { get; set; } = new();
        protected List<int> QuestionIds { get; set; } = new();


        protected string StudentName { get; set; } = "Nguyễn Văn A";
        protected string StudentCode { get; set; } = "20123456";
        protected int DurationMinutes { get; set; } = 90;
        protected DateTime StartTime { get; set; } = DateTime.Now;

        protected override void OnInitialized()
        {
            var mock = Mock.GetMockExam();
            DurationMinutes = mock.OriginalExamPaper.DurationMinutes;
            // Duyệt theo thứ tự Order và tạo dãy hiển thị: cha, rồi đến từng con
            FlatQuestions = new List<QuestionStructureDto>();
            foreach (var q in mock.ExamPaper.QuestionStructures.OrderBy(x => x.Order))
            {
                FlatQuestions.Add(q);
                if (q.ChildQuestions != null && q.ChildQuestions.Count > 0)
                {
                    FlatQuestions.AddRange(q.ChildQuestions.OrderBy(c => c.Order));
                }
            }
            // Build id list for navigation
            QuestionIds = FlatQuestions.Select(q => q.OriginalExamPaperDetailId).ToList();
            AnsweredMapDerived = ComputeAnsweredWithGroups();
        }

        private static List<QuestionStructureDto> FlattenQuestions(List<QuestionStructureDto> items)
        {
            var list = new List<QuestionStructureDto>();
            foreach (var q in items.OrderBy(x => x.Order))
            {
                list.Add(q);
                // KHÔNG chèn trực tiếp con ở đây, vì chúng ta render danh sách phẳng theo thứ tự tự nhiên đã có trong DTO (Order)
            }
            return list;
        }

        protected void MarkFlag()
        {
            // could toggle a flag map; simplified here
        }

        protected void OnAnswered((int questionId, object? value) payload)
        {
            AnsweredMap[payload.questionId] = payload.value;
            AnsweredMapDerived = ComputeAnsweredWithGroups();
        }

        protected void SubmitExam()
        {
            // Collect answers from AnsweredMap and submit
        }

        protected async Task ScrollTo(int index)
        {
            if (index < 0 || index >= FlatQuestions.Count) return;
            var id = $"q-{FlatQuestions[index].OriginalExamPaperDetailId}";
            await JS.InvokeVoidAsync("scrollToElement", id);
        }

        private Dictionary<int, object?> ComputeAnsweredWithGroups()
        {
            var result = new Dictionary<int, object?>(AnsweredMap);
            foreach (var q in FlatQuestions)
            {
                if (q.ChildQuestions != null && q.ChildQuestions.Count > 0)
                {
                    var allChildIds = q.ChildQuestions.Select(c => c.OriginalExamPaperDetailId).ToList();
                    var allAnswered = allChildIds.All(id => AnsweredMap.ContainsKey(id));
                    if (allAnswered)
                    {
                        result[q.OriginalExamPaperDetailId] = true;
                    }
                }
            }
            return result;
        }
    }
}