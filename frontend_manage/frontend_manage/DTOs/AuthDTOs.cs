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
} 