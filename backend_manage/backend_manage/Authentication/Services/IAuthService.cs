
namespace backend_manage.Authentication.Services;

public interface IAuthService
{
    Task<AuthResultDto> AuthenticateAsync(LoginModelDto loginModel);
    Task LogoutAsync(string token);

}