namespace frontend_manage.DTOs
{
    public class UpdateOriginalExamPaperRequest
    {
        public string OriginalExamPaperCore { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SubjectId { get; set; }
        public bool AllowViewMaterials { get; set; }
        public int DurationMinutes { get; set; }
        public bool? IsApproved { get; set; }
    }
}

