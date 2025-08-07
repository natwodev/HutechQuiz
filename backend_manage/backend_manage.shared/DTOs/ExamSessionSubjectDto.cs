namespace backend_manage.shared.DTOs
{
    public class ExamSessionSubjectDto
    {
        public int ExamSessionSubjectId { get; set; }
        public int ExamSessionDepartmentId { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }
        public int Duration { get; set; }
        public int? OriginalExamPaperId { get; set; }
        public string OriginalExamPaperTitle { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ExamSessionSubjectCore { get; set; }
    }

    public class ExamSessionSubjectCreateDto
    {
        public int ExamSessionDepartmentId { get; set; }
        public int SubjectId { get; set; }
        public int Duration { get; set; }
        public int? OriginalExamPaperId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ExamSessionSubjectCore { get; set; }
    }

    public class ExamSessionSubjectUpdateDto
    {
        public int ExamSessionDepartmentId { get; set; }
        public int SubjectId { get; set; }
        public int Duration { get; set; }
        public int? OriginalExamPaperId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ExamSessionSubjectCore { get; set; }
    }
} 