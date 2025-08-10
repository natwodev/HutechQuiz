using backend_manage.core.Authentication.Repositories;
using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Middlewares.Jwt;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace backend_manage.core.Authentication.Services
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
            
            // ✅ Lấy role giống như login thường
            var roles = await _userRepository.GetUserRolesAsync(user);
            // Tạo JWT với thông tin userName và roles
            var tokenString = _jwtTokenGenerator.GenerateJwtToken(user.Id, roles);
            return new AuthResultDto
            {
                IsSuccess = true,
                Token = tokenString
            };
        }

        // Phương thức xác thực cho cookie
        public async Task<AuthResultDto> AuthenticateForCookieAsync(LoginModelDto loginModel)
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

            // Chỉ kiểm tra mật khẩu (không sign-in ở đây để tự kiểm soát claims và tránh tạo nhiều cookie)
            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, loginModel.Password, true);
            if (!signInResult.Succeeded)
            {
                return new AuthResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "Sai tài khoản hoặc mật khẩu."
                };
            }

            // Lấy roles của user
            var roles = await _userRepository.GetUserRolesAsync(user);
            
            // Tạo claims cho user
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.UserName),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email ?? "")
            };
            
            // Thêm roles vào claims
            foreach (var role in roles)
            {
                claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
            }

            return new AuthResultDto
            {
                IsSuccess = true,
                Token = null // Không cần token cho cookie auth
            };
        }

        // Lấy thông tin user
        public async Task<ApplicationUser?> GetUserByUsernameAsync(string userName)
        {
            return await _authRepository.GetUserByUsernameAsync(userName);
        }

        // Lấy roles của user
        public async Task<List<string>> GetUserRolesAsync(ApplicationUser user)
        {
            return await _userRepository.GetUserRolesAsync(user);
        }

        // Đăng xuất cookie
        public async Task SignOutAsync()
        {
            await _signInManager.SignOutAsync();
        }
        
        // Đăng nhập với claims
        public async Task SignInWithClaimsAsync(ApplicationUser user, List<System.Security.Claims.Claim> claims)
        {
            var authProps = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true,
                IssuedUtc = DateTimeOffset.UtcNow
            };

            await _signInManager.SignInWithClaimsAsync(user, authProps, claims);
        }
        
        public async Task LogoutAsync(string token)
        {
            // Token blacklist functionality has been removed
            // Logout now only logs the action without blacklisting the token
            _logger.LogInformation("User logged out successfully");
        }
    }
}
   