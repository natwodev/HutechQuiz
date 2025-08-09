using backend_manage.core.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace backend_manage.core.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<List<string>> GetUserRolesAsync(ApplicationUser user);
        Task<ApplicationUser?> FindByEmailAsync(string email);
    }
}
