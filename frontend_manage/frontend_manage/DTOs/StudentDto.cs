namespace frontend_manage.DTOs
{
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
        public int StudentExamSessionId { get; set; }
        public int Duration { get; set; }
        public int ExtraMinutes { get; set; }
        public double Score { get; set; } = 0;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class SubjectExamRoomStatusDto
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public string RoomName { get; set; }
        public int Duration { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsActive { get; set; }
        public DateTime ExamSessionStartTime { get; set; }
        public DateTime ExamSessionEndTime { get; set; }
        public string ExamSessionName { get; set; }
        public string LecturerCode { get; set; }
    }

    public class StudentListResponse
    {
        public SubjectExamRoomStatusDto Subject { get; set; }
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
    
    
    public class StudentInfoDto
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public string DateOfBirth { get; set; }
        public bool? IsLogin { get; set; }
        public DateTime? LastLoggedIn { get; set; }
        public DateTime? LastLoggedOut { get; set; }
        // public object StudentExamSessions { get; set; } // Có thể tạo class riêng nếu cần
        public DateTime? CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public bool? IsDeleted { get; set; }
        public int? Version { get; set; }
    } 
    
    
    
    
    public class StudentExamSessionCacheDto
    {
        // Từ Entity
        public int StudentExamSessionId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string StudentCode { get; set; }
        public int StudentId { get; set; }
        public int ExamSessionSubjectId { get; set; }
        public int? ShuffledExamPaperId { get; set; }
        public int ExtraMinutes { get; set; }
        public string? ReasonForExtra { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalQuestions { get; set; }
        public double Score { get; set; }
        public bool IsCompleted { get; set; }
        public string StudentAnswersString { get; set; }
        public int? ExamRoomId { get; set; }
    
        // Từ DTO (những trường Entity không có)
        public string SubjectName { get; set; }
        public string RoomName { get; set; }
        public int Duration { get; set; }
        
    }


}
