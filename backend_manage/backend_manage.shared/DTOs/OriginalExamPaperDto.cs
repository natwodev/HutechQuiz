namespace backend_manage.shared.DTOs
{
    public class OriginalExamPaperDto
    {
        public int OriginalExamPaperId { get; set; }
        public string OriginalExamPaperCore { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int SubjectId { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public List<OriginalExamPaperDetailDto> Details { get; set; }
    }
     public class OriginalExamPaperDetailDto
    {
        public int OriginalExamPaperDetailId { get; set; }
        public int Order { get; set; }
        public string? QuestionContent { get; set; }
        public string? Answer1 { get; set; }
        public string? Answer2 { get; set; }
        public string? Answer3 { get; set; }
        public string? Answer4 { get; set; }
        public int? CorrectAnswerIndex { get; set; }
        public int? ParentQuestionId { get; set; }
        public int ChapterId { get; set; }
        public bool CanShuffleQuestion { get; set; }
        public string? AnswerShuffleInfo { get; set; }
        public OriginalExamPaperDetailDto? ParentQuestion { get; set; }
        public List<OriginalExamPaperDetailDto> ChildQuestions { get; set; } = new();
    }
} 