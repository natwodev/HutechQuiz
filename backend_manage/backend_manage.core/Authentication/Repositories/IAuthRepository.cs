using backend_manage.core.Entities;

namespace backend_manage.core.Authentication.Repositories
{
    public interface IAuthRepository
    {
        // Method to get user by ID
        Task<ApplicationUser?> GetUserByUsernameAsync(string username);
        // Method to get permissions by user ID
    }
}