using backend_manage.core.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace backend_manage.core.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<string>> GetUserRolesAsync(string userId);
        // Phương thức RoleClaim
        Task<ApplicationUser?> FindByEmailAsync(string email);
    }
}
