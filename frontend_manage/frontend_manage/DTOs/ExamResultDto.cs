namespace frontend_manage.DTOs
{
    public class ExamResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ExamResultDataDto Data { get; set; } = new();
    }

    public class ExamResultDataDto
    {
        public string StudentCode { get; set; } = string.Empty;
        public int ShuffledExamPaperId { get; set; }
        public int Score { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string StudentAnswersString { get; set; } = string.Empty;
        public string AnswerKey { get; set; } = string.Empty;
    }
}

