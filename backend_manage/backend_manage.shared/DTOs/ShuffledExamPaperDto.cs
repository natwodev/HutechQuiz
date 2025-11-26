namespace backend_manage.shared.DTOs
{
    public class ShuffledExamPaperDto
    {
        public int ShuffledExamPaperId { get; set; }
        public string ShuffledExamPaperCore { get; set; }
        public string Title { get; set; }
        public int OriginalExamPaperId { get; set; }
        public int SubjectId { get; set; }
        public bool AllowViewMaterials { get; set; }
        public bool IsApproved { get; set; }
        public string AnswerKey { get; set; }
        public string SubjectName { get; set; }
        public string SubjectCode { get; set; }
        public int? ExamSessionSubjectId { get; set; }
        
        //public string? QuestionStructure { get; set; }
        
        public List<QuestionStructureDto> QuestionStructures { get; set; } = new();
        
    }
    
} 