using System.Collections.Generic;

namespace backend_manage.DTOs
{
    public class ShuffledExamPaperDto
    {
        public int ShuffledExamPaperId { get; set; }
        public string ShuffledExamPaperCore { get; set; }
        public string Title { get; set; }
        public int OriginalExamPaperId { get; set; }
        public int? ExamSessionSubjectId { get; set; }
        public int SubjectId { get; set; }
        public bool IsApproved { get; set; }
        public string AnswerKey { get; set; }
        public List<ShuffledExamPaperDetailDto> Details { get; set; }
    }
} 