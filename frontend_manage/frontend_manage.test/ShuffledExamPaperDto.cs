namespace frontend_manage.test;

public class ShuffledExamPaperDto
{
    public int ShuffledExamPaperId { get; set; }
    public string ShuffledExamPaperCore { get; set; }
    public string Title { get; set; }
    public int OriginalExamPaperId { get; set; }
    public int SubjectId { get; set; }
    public bool IsApproved { get; set; }
    public string AnswerKey { get; set; }
    public string SubjectName { get; set; }
    public string SubjectCode { get; set; }
    public int? ExamSessionSubjectId { get; set; }
    public List<ShuffledExamPaperDetailDto> Details { get; set; }
}

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

public class StartExamResponseDto
{
    //public StudentExamSessionCacheDto StudentSession { get; set; }
    public ShuffledExamPaperDto ExamPaper { get; set; }
} 