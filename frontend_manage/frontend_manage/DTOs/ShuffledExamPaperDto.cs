namespace frontend_manage.DTOs;

public class ShuffledExamPaperDto
{
    public int ShuffledExamPaperId { get; set; }
    public string ShuffledExamPaperCore { get; set; }
    public string Title { get; set; }
    public bool AllowViewMaterials { get; set; }
    public int OriginalExamPaperId { get; set; }
    public int SubjectId { get; set; }
    public bool IsApproved { get; set; }
    public string AnswerKey { get; set; }
    public string SubjectName { get; set; }
    public string SubjectCode { get; set; }
    public int? ExamSessionSubjectId { get; set; }
        
    //public string? QuestionStructure { get; set; }
        
    public List<QuestionStructureDto> QuestionStructures { get; set; } = new();
        
}

public class QuestionStructureDto
{

    public int OriginalExamPaperDetailId { get; set; }
        
    public int? ParentQuestionId { get; set; }
        
    public int Order { get; set; }
    public string? QuestionContent { get; set; }
    public List<QuestionStructureDto> ChildQuestions { get; set; } = new();
        
    public List<AnswerStructureDto> Answers { get; set; } = new();
}
    
public class AnswerStructureDto
{
    public int AnswerId { get; set; }
        
    public int Order { get; set; }
    public string AnswerContent { get; set; }
        
    public int OriginalExamPaperDetailId { get; set; }
}

public class OriginalExamPaperDto
{
    public int OriginalExamPaperId { get; set; }
    public string OriginalExamPaperCore { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public int SubjectId { get; set; }
    public bool AllowViewMaterials { get; set; }
    public int DurationMinutes { get; set; }
    public int TotalQuestions { get; set; }
    public string? KeyValueList { get; set; }
    public List<OriginalExamPaperDetailDto> Details { get; set; } = new();
}

public class OriginalExamPaperDetailDto
{
    public int OriginalExamPaperDetailId { get; set; }
    public int Order { get; set; }
    public string? QuestionContent { get; set; }
    public int? CorrectAnswerIndex { get; set; }
    public int? ParentQuestionId { get; set; }
    public int ChapterId { get; set; }
    public bool CanShuffleQuestion { get; set; }
    public string? AnswerShuffleInfo { get; set; }
    public OriginalExamPaperDetailDto? ParentQuestion { get; set; }
    public List<OriginalExamPaperDetailDto> ChildQuestions { get; set; } = new();
    public List<AnswerDto> Answers { get; set; } = new();
}

public class AnswerDto
{
    public int AnswerId { get; set; }
    public int Order { get; set; }
    public string AnswerContent { get; set; }
    public bool IsCorrect { get; set; }
    public bool CanShuffleAnswer { get; set; }
    public int OriginalExamPaperDetailId { get; set; }
}

public class StartExamResponseDto
{
    public StudentExamSessionCacheDto StudentSession { get; set; }
    public ShuffledExamPaperDto ExamPaper { get; set; }
    public OriginalExamPaperDto OriginalExamPaper { get; set; }
} 


