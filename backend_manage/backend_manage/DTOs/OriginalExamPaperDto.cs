using System.Collections.Generic;

namespace backend_manage.DTOs
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
} 