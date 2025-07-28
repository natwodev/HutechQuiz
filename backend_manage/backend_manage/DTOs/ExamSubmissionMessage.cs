using System;

namespace backend_manage.DTOs
{
    public class ExamSubmissionMessage
    {
        public string StudentCode { get; set; }
        public int ShuffledExamPaperId { get; set; }
        public double Score { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime EndTime { get; set; }
        public string StudentAnswersString { get; set; }
    }
} 