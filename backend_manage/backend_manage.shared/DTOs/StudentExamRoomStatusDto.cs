namespace backend_manage.shared.DTOs
{
    public class StudentExamRoomStatusDto
    {
        public string StudentCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsLogin { get; set; }
        public bool IsCompleted { get; set; }
        public int? ExamSessionSubjectId { get; set; }
        public string? SubjectName { get; set; }
        public int Duration { get; set; }
        public int ExtraMinutes { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime ExamSessionStartTime { get; set; }
        public DateTime ExamSessionEndTime { get; set; }
    }
} 