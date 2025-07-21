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
    public int ExamSessionSubjectId { get; set; }
    public string SubjectName { get; set; }
    public string RoomName { get; set; }
    public int Duration { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
} 