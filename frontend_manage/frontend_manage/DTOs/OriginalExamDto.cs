namespace frontend_manage.DTOs
{
    public class OriginalExamDto
    {
        public int OriginalExamPaperId { get; set; }
        public string OriginalExamPaperCore { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public bool? IsApproved { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalShuffledPapers { get; set; }
        public string? KeyValueList { get; set; }
    }
}

