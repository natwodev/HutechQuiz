namespace frontend_manage.DTOs
{
    public class StudentDto
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public ExamStatus ExamStatus { get; set; }
        public LoginStatus LoginStatus { get; set; }
        public int ExtraTime { get; set; }
        public double? Score { get; set; }
    }

    public enum ExamStatus
    {
        NotStarted,
        TakingExam,
        Submitted
    }

    public enum LoginStatus
    {
        NotLoggedIn,
        LoggedIn
    }
    
    
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
    }

    public class StudentListResponse
    {
        public List<StudentExamRoomStatusDto> Students { get; set; }
    }
    
    public class StudentExamSessionDto
    {
        public int ExamSessionSubjectId { get; set; }
        public string SubjectName { get; set; } 
        public string RoomName { get; set; } 
        public int Duration { get; set; }
        public int ExtraMinutes { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int StudentExamSessionId { get; set; }
        public int? ShuffledExamPaperId { get; set; }
        public string StudentAnswersString { get; set; }
        public DateTime ExamSessionStartTime { get; set; }
        public DateTime ExamSessionEndTime { get; set; }
    }
}
