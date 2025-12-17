namespace backend_manage.shared.DTOs
{
    public class StudentDto
    {
       // public int StudentId { get; set; }
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        //public string Username { get; set; }
        public bool IsLogin { get; set; }
        public bool? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string DepartmentId { get; set; }
        public string? UserId { get; set; }
    }

    public class StudentCreateDto
    {
        public string StudentCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class StudentUpdateDto
    {
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string DepartmentId { get; set; }
        public string? UserId { get; set; }
    }


    public class StudentExamSessionHistoryDto
    {
        public int StudentExamSessionId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string StudentCode { get; set; }
        public string? SubjectName { get; set; } 
        public string? RoomName { get; set; } 
        public string? ExamSessionName { get; set; }
        public int StudentId { get; set; }
        public int ExamSessionSubjectId { get; set; }
        public int? OriginalExamPaperId { get; set; }
        public int? ShuffledExamPaperId { get; set; }
        public int ExtraMinutes { get; set; }
        public string? ReasonForExtra { get; set; }
        public int? RemainingMinutes { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalQuestions { get; set; }
        public double Score { get; set; }
        public bool IsCompleted { get; set; }
        public string? StudentAnswersString { get; set; }
        public DateTime ExamSessionStartTime { get; set; }
        public DateTime ExamSessionEndTime { get; set; }
    }

} 
