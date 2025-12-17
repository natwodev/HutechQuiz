namespace backend_manage.shared.DTOs
{

    public class StudentExamSessionCacheDto
    {
        // Từ Entity
        public int StudentExamSessionId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string StudentCode { get; set; }
        public int StudentId { get; set; }
        public int ExamSessionSubjectId { get; set; }
        public int? OriginalExamPaperId { get; set; }
        public int? ShuffledExamPaperId { get; set; }
        public int ExtraMinutes { get; set; }
        public string? ReasonForExtra { get; set; }
        public int RemainingMinutes { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalQuestions { get; set; }
        public double Score { get; set; }
        public bool IsCompleted { get; set; }
        public string StudentAnswersString { get; set; }
        public int? ExamRoomId { get; set; }
        
        // Từ DTO (những trường Entity không có)
        public string SubjectName { get; set; }
        public string RoomName { get; set; }
        public string ExamSessionName { get; set; }
        public int Duration { get; set; }
        
        // Thời gian được phép làm bài (cached từ ExamSessionSubject)
        public DateTime ExamSessionStartTime { get; set; }
        public DateTime ExamSessionEndTime { get; set; }
        
        // Không cần các trường audit từ BaseEntity cho cache
    }
} 