using System.Threading.Tasks;
using backend_manage.Authentication.Repositories;
using backend_manage.Entities;
using backend_manage.Hubs;
using backend_manage.Middlewares.Jwt;
using backend_manage.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace backend_manage.Authentication.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly JwtTokenGenerator _jwtTokenGenerator;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IAuthRepository authRepository, 
            SignInManager<ApplicationUser> signInManager, 
            IConfiguration configuration,
            RoleManager<IdentityRole> roleManager,
            IUserRepository userRepository,
            ILogger<AuthService> logger)
        {
            _authRepository = authRepository;
            _signInManager = signInManager;
            _jwtTokenGenerator = new JwtTokenGenerator(configuration);
            _roleManager = roleManager;
            _userRepository = userRepository;
            _logger = logger;
        }

        // Phương thức xác thực
        public async Task<AuthResultDto> AuthenticateAsync(LoginModelDto loginModel)
        {
            // Tìm kiếm người dùng theo tên đăng nhập
            var user = await _authRepository.GetUserByUsernameAsync(loginModel.UserName);

            if (user == null)
            {
                return new AuthResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "Tài khoản không tồn tại"
                };
            }
            
            // Kiểm tra tài khoản có bị khóa không
            if (user.LockoutEnabled && user.LockoutEnd > DateTimeHelper.GetVietnamTime())
            {
                return new AuthResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "Tài khoản đã bị khóa. Vui lòng liên hệ phòng đào tạo để biết thêm chi tiết!"
                };
            }
            // Kiểm tra mật khẩu với SignInManager (bạn có thể bỏ qua phần này nếu không cần)
            var signInResult = await _signInManager.PasswordSignInAsync(user, loginModel.Password, false, true);
            if (!signInResult.Succeeded)
            {
                return new AuthResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "Sai tài khoản hoặc mật khẩu."
                };
            }
            
            // ✅ Lấy role và permission giống như login thường
            var roles = await _userRepository.GetUserRolesAsync(user);
            var permissions = await _userRepository.GetUserPermissionsAsync(user);
            // Tạo JWT với thông tin userName, roles và permissions
            var tokenString = _jwtTokenGenerator.GenerateJwtToken(user.Id, roles, permissions);
            return new AuthResultDto
            {
                IsSuccess = true,
                Token = tokenString
            };
        }
        
        public async Task LogoutAsync(string token)
        {
            // Token blacklist functionality has been removed
            // Logout now only logs the action without blacklisting the token
            _logger.LogInformation("User logged out successfully");
        }
    }
}
   