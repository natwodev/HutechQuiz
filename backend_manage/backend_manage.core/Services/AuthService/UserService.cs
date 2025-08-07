using backend_manage.core.Entities;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.Interfaces;
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
        
        
        public AuthenticationProperties ConfigureExternalAuthenticationProperties(string provider, string redirectUrl)
        {
            return _userRepository.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        }

        public async Task<ExternalLoginInfo?> GetExternalLoginInfoAsync()
        {
            return await _userRepository.GetExternalLoginInfoAsync();
        }
        
        public async Task<IdentityResult> AddLoginAsync(ApplicationUser user, ExternalLoginInfo info)
        {
            return await _userRepository.AddLoginAsync(user, info);
        }

        public async Task<ApplicationUser?> FindByLoginAsync(string loginProvider, string providerKey)
        {
            return await _userRepository.FindByLoginAsync(loginProvider, providerKey);
        }
        
        public async Task<List<string>> GetUserPermissionsAsync(ApplicationUser user)
        {
            return await _userRepository.GetUserPermissionsAsync(user);
        }
    }
}
