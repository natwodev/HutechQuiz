
using backend_manage.shared.DTOs;

namespace backend_manage.core.Authentication.Services;

public interface IAuthService
{
    Task<AuthResultDto> AuthenticateAsync(LoginModelDto loginModel);
    Task LogoutAsync(string token);

}