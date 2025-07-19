namespace backend_manage.DTOs
{
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
        public List<OriginalExamPaperDetailDto> ChildQuestions { get; set; } = new();
    }
} 