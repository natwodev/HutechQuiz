using backend_manage.core.Entities;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace backend_manage.core.Services.AuthService
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
  
        

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
          
        }


        public async Task<List<string>> GetUserRolesAsync(string userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return new List<string>();

            return await _userRepository.GetUserRolesAsync(user);
        }

     
        public async Task<ApplicationUser?> FindByEmailAsync(string email)
        {
            return await _userRepository.FindByEmailAsync(email);
        }
    }
}
