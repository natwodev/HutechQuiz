namespace backend_manage.DTOs
{
    public class ShuffledExamPaperDetailDto
    {
        public int ShuffledExamPaperDetailId { get; set; }
        public int Order { get; set; }
        public string AnswerOrder { get; set; }
        public int OriginalExamPaperDetailId { get; set; }
        public int? ParentQuestionId { get; set; }
        public string QuestionContent { get; set; }
        public string Answer1 { get; set; }
        public string Answer2 { get; set; }
        public string Answer3 { get; set; }
        public string Answer4 { get; set; }
        public List<ShuffledExamPaperDetailDto> ChildQuestions { get; set; } = new();
    }
} 