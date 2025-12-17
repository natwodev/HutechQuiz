namespace backend_manage.shared.DTOs
{
    public class OriginalExamPaperDto
    {
        public int OriginalExamPaperId { get; set; }
        public string OriginalExamPaperCore { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int SubjectId { get; set; }
        public bool AllowViewMaterials { get; set; }
        public bool IsManualCreated { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public string? KeyValueList { get; set; }
        public List<OriginalExamPaperDetailDto> Details { get; set; }
    }
     public class OriginalExamPaperDetailDto
    {
        public int OriginalExamPaperDetailId { get; set; }
        public int Order { get; set; }
        public string? QuestionContent { get; set; }
        public int? CorrectAnswerIndex { get; set; }
        public int? ParentQuestionId { get; set; }
        public int? ChapterId { get; set; }
        public bool CanShuffleQuestion { get; set; }
        public string? AnswerShuffleInfo { get; set; }
        public OriginalExamPaperDetailDto? ParentQuestion { get; set; }
        public List<OriginalExamPaperDetailDto> ChildQuestions { get; set; } = new();
        public List<AnswerDto> Answers { get; set; } = new();
    }

    public class OriginalExamDto
    {
        public int OriginalExamPaperId { get; set; }
        public string OriginalExamPaperCore { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }
        public bool? IsApproved { get; set; }
        public bool AllowViewMaterials { get; set; }
        public bool IsManualCreated { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalShuffledPapers { get; set; }
        public string? KeyValueList { get; set; }
    }

    public class OriginalExamPaperDetailShufferDto
    {
        public int OriginalExamPaperDetailId { get; set; }
        public int Order { get; set; }
        public string? QuestionContent { get; set; }
        public int? CorrectAnswerIndex { get; set; }
        public int? ParentQuestionId { get; set; }
        public bool CanShuffleQuestion { get; set; }
        public string? AnswerShuffleInfo { get; set; } 
    }

    public class CreateOriginalExamPaperRequest
    {
        public string OriginalExamPaperCore { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int SubjectId { get; set; }
        public bool AllowViewMaterials { get; set; }
        public int DurationMinutes { get; set; }
        public bool? IsApproved { get; set; }
    }

    public class UpdateOriginalExamPaperRequest
    {
        public string OriginalExamPaperCore { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int SubjectId { get; set; }
        public bool AllowViewMaterials { get; set; }
        public int DurationMinutes { get; set; }
        public bool? IsApproved { get; set; }
    }
    
} 