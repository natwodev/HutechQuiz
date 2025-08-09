using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs;

public class LoginModelDto
{
    public string UserName { get; set; }
    public string Password { get; set; } 
}

public class StudentLoginRequestDto
{
    [Required(ErrorMessage = "Vui lòng nhập mã sinh viên")]
    [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Mã sinh viên chỉ được chứa chữ cái và số")]
    public string StudentCode1 { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Mật khẩu chỉ được chứa chữ cái và số")]
    public string StudentCode2 { get; set; } = string.Empty;
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

