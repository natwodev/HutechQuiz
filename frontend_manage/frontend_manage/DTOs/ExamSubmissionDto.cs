namespace frontend_manage.DTOs
{
    public class ExamSubmissionDto
    {
        public string StudentCode { get; set; }
        public int ShuffledExamPaperId { get; set; }
        public double? Score { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalQuestions { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string StudentAnswersString { get; set; }
        public string AnswerKey { get; set; }
    }
}

