
using backend_manage.core.Entities;
using backend_manage.shared.DTOs;

namespace backend_manage.core.Authentication.Services;

public interface IAuthService
{
    Task<AuthResultDto> AuthenticateAsync(LoginModelDto loginModel);
    Task<AuthResultDto> AuthenticateForCookieAsync(LoginModelDto loginModel);
    Task<ApplicationUser?> GetUserByUsernameAsync(string userName);
    Task<List<string>> GetUserRolesAsync(ApplicationUser user);
    Task SignOutAsync();
    Task LogoutAsync(string token);
}