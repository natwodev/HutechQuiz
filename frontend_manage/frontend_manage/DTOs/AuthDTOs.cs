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

