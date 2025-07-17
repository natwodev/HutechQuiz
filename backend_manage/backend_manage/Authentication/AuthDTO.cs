namespace backend_manage.Authentication.Services;

public class AuthResultDto
{
    public string Token { get; set; }
    public string Role { get; set; }
    public bool IsSuccess { get; set; }  // Thêm dòng này
    public string ErrorMessage { get; set; }  // Và dòng này
}
public class LoginModelDto
{
    public string UserName { get; set; }
    public string Password { get; set; }  // Lưu mật khẩu đã hash
    
}
