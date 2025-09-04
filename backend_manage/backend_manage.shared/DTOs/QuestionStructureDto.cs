namespace backend_manage.shared.DTOs
{
  
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
    
    
    
    public class OriginalExamPaperrdDto
    {
        public int OriginalExamPaperId { get; set; }
        public string OriginalExamPaperCore { get; set; }
        public string? KeyValueList { get; set; }
        public List<OriginalExamPaperDetailrdDto> Details { get; set; }
    }
    public class OriginalExamPaperDetailrdDto
    {
        public int OriginalExamPaperDetailId { get; set; }
        public string? QuestionContent { get; set; }
        public int? ParentQuestionId { get; set; }
        public OriginalExamPaperDetailDto? ParentQuestion { get; set; }
        public List<OriginalExamPaperDetailDto> ChildQuestions { get; set; } = new();
        public List<AnswerrdDto> Answers { get; set; } = new();
    }
    
    public class AnswerrdDto
    {
        public int AnswerId { get; set; }
        public string AnswerContent { get; set; }
    }
}
