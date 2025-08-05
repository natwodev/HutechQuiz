namespace FrontEnd.DTOs;

public class LoginModelDto
{
    public string UserName { get; set; }
    public string Password { get; set; } 
}

public class AuthResultDto
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
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

public class ExamSessionDto
{
    public int StudentExamSessionId { get; set; }
    public int ExamSessionSubjectId { get; set; }
    public string SubjectName { get; set; }
    public string RoomName { get; set; }
    public int Duration { get; set; }
    public int ExtraMinutes { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
} 

public class ShuffledExamPaperDto
{
    public int ShuffledExamPaperId { get; set; }
    public string ShuffledExamPaperCore { get; set; }
    public string Title { get; set; }
    public int OriginalExamPaperId { get; set; }
    public int SubjectId { get; set; }
    public bool IsApproved { get; set; }
    public string AnswerKey { get; set; }
    public string SubjectName { get; set; }
    public string SubjectCode { get; set; }
    public int? ExamSessionSubjectId { get; set; }
    public List<ShuffledExamPaperDetailDto> Details { get; set; }
}

public class ShuffledExamPaperDetailDto
{
    public int ShuffledExamPaperDetailId { get; set; }
    public int Order { get; set; }
    public string AnswerOrder { get; set; }
    public int OriginalExamPaperDetailId { get; set; }
    public int? ParentQuestionId { get; set; }
    public string QuestionContent { get; set; }
    public string Answer1 { get; set; }
    public string Answer2 { get; set; }
    public string Answer3 { get; set; }
    public string Answer4 { get; set; }
    public List<ShuffledExamPaperDetailDto> ChildQuestions { get; set; } = new();
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
    
    // Audit fields từ BaseEntity
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public int Version { get; set; }
}

public class StartExamResponseDto
{
    public StudentExamSessionCacheDto StudentSession { get; set; }
    public ShuffledExamPaperDto ExamPaper { get; set; }
} 