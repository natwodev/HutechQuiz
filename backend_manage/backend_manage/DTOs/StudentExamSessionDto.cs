using System;

namespace backend_manage.DTOs
{
    public class StudentExamSessionDto
    {
        public int StudentExamSessionId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int StudentId { get; set; }
        public int ExamSessionSubjectId { get; set; }
        public int? ShuffledExamPaperId { get; set; }
        public int ExtraMinutes { get; set; }
        public string? ReasonForExtra { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalQuestions { get; set; }
        public double Score { get; set; }
        public bool IsCompleted { get; set; }
        public int? ExamRoomId { get; set; }
        public string StudentAnswersString { get; set; }
    }
} 