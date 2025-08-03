using System.Threading.Tasks;
using backend_manage.Entities;

namespace backend_manage.Authentication.Repositories
{
    public interface IAuthRepository
    {
        // Method to get user by ID
        Task<ApplicationUser?> GetUserByUsernameAsync(string username);
        // Method to get permissions by user ID
    }
}