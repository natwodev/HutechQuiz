namespace backend_manage.shared.DTOs;

public class StudentAuthResultDto
{
    public string Token { get; set; }
    public string Role { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
} 