namespace backend_manage.shared.DTOs
{
  
    public class QuestionStructureDto
    {

        public int OriginalExamPaperDetailId { get; set; }
        
        public int? ParentQuestionId { get; set; }
        
        public int Order { get; set; }
        
        public List<QuestionStructureDto> ChildQuestions { get; set; } = new();
        
        public List<AnswerStructureDto> Answers { get; set; } = new();
    }
    
    public class AnswerStructureDto
    {
        public int AnswerId { get; set; }
        
        public int Order { get; set; }
        
        public int OriginalExamPaperDetailId { get; set; }
    }
}
