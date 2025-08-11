
using backend_manage.core.Entities;
using backend_manage.shared.DTOs;

namespace backend_manage.core.Authentication.Services;

public interface IAuthService
{
    Task<AuthResultDto> AuthenticateAsync(LoginModelDto loginModel);

    Task<ApplicationUser?> GetUserByUsernameAsync(string userName);
    Task<List<string>> GetUserRolesAsync(ApplicationUser user);

    Task LogoutAsync(string token);
}