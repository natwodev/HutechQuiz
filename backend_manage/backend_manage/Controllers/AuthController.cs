using System.Security.Claims;
using backend_manage.core.Authentication.Services;
using backend_manage.core.Entities;
using backend_manage.core.Middlewares.Jwt;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using backend_manage.shared.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using Microsoft.AspNetCore.Authorization;

namespace backend_manage.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;
        private readonly JwtTokenGenerator _jwtTokenGenerator;
        private readonly SignInManager<ApplicationUser> _signInManager;

        
        public AuthController(IAuthService authService, IConfiguration configuration, IUserService userService, SignInManager<ApplicationUser> signInManager)
        {
            _authService = authService;
            _configuration = configuration;
            _userService = userService;
            _jwtTokenGenerator = new JwtTokenGenerator(configuration);
            _signInManager = signInManager;
        }
        
        // Đăng nhập JWT
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModelDto loginModel)
        {
            try
            {
                var authResult = await _authService.AuthenticateAsync(loginModel);

                if (!authResult.IsSuccess)
                {
                    return BadRequest(new { message = authResult.ErrorMessage });
                }

                return Ok(new
                {
                    token = authResult.Token
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau." });
            }
        }
       
        // Đăng nhập Identity với Cookie
        [HttpPost("login-cookie")]
        public async Task<IActionResult> LoginWithCookie([FromBody] LoginModelDto loginModel)
        {
            try
            {
                // Đăng xuất session hiện tại trước khi tạo session mới (để tránh xung đột)
               // await _signInManager.SignOutAsync();
                
                // Sử dụng AuthService để xác thực và tạo cookie
                var authResult = await _authService.AuthenticateForCookieAsync(loginModel);
                
                if (!authResult.IsSuccess)
                {
                    return BadRequest(new { message = authResult.ErrorMessage });
                }

                // Lấy thông tin user
                var user = await _authService.GetUserByUsernameAsync(loginModel.UserName);
                if (user == null)
                {
                    return BadRequest(new { message = "Tên đăng nhập hoặc mật khẩu không đúng." });
                }
                
                // Lấy roles
                var roles = await _authService.GetUserRolesAsync(user);

                // Đăng nhập với user (Identity sẽ tự động tạo claims cần thiết)
                await _signInManager.SignInAsync(user, isPersistent: true);
                
                // Log để debug
                Console.WriteLine($"Signed in user: {user.UserName} with roles: {string.Join(", ", roles)}");

                return Ok(new
                {
                    message = "Đăng nhập thành công",
                    user = new
                    {
                        id = user.Id,
                        username = user.UserName,
                        email = user.Email,
                        roles = roles
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau." });
            }
        }

        // Đăng xuất Cookie
        [HttpPost("logout-cookie")]
        public async Task<IActionResult> LogoutWithCookie()
        {
            try
            {
                await _authService.SignOutAsync();
                return Ok(new { message = "Đăng xuất thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi đăng xuất." });
            }
        }
        
        
        // Đăng xuất JWT
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Headers["Authorization"]
                .FirstOrDefault()
                ?.Split(" ")
                .Last();

            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { message = "Token is required" });
            }
        
            try
            {
                await _authService.LogoutAsync(token);
                return Ok(new { message = "Successfully logged out" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Kiểm tra trạng thái đăng nhập
        [HttpGet("check-auth")]
        public async Task<IActionResult> CheckAuthentication()
        {
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var user = await _authService.GetUserByUsernameAsync(User.Identity.Name);
                        if (user != null)
                        {
                            var roles = await _authService.GetUserRolesAsync(user);

                            return Ok(new
                            {
                                isAuthenticated = true,
                                user = new
                                {
                                    id = user.Id,
                                    username = user.UserName,
                                    email = user.Email,
                                    roles = roles
                                }
                            });
                        }
                    }
                }

                return Ok(new { isAuthenticated = false });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi kiểm tra trạng thái đăng nhập." });
            }
        }

        // Test endpoint để kiểm tra cookie authentication
        [HttpGet("test-cookie")]
        [Authorize] // Yêu cầu đăng nhập
        public IActionResult TestCookieAuth()
        {
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var username = User.Identity.Name;
                    var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
                    
                    // Debug: Lấy tất cả claims để kiểm tra
                    var allClaims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();

                    return Ok(new
                    {
                        message = "Cookie authentication hoạt động!",
                        isAuthenticated = true,
                        userId = userId,
                        username = username,
                        roles = roles,
                        allClaims = allClaims, // Debug info
                        cookieName = Request.Cookies["HutechQuiz.Auth"],
                        authScheme = User.Identity.AuthenticationType
                    });
                }

                return Unauthorized(new { message = "Chưa đăng nhập" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi: " + ex.Message });
            }
        }

        // Lấy thông tin user hiện tại (endpoint /me)
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var username = User.Identity.Name;
                    
                    if (!string.IsNullOrEmpty(username))
                    {
                        var user = await _authService.GetUserByUsernameAsync(username);
                        if (user != null)
                        {
                            var roles = await _authService.GetUserRolesAsync(user);

                            return Ok(new
                            {
                                id = user.Id,
                                username = user.UserName,
                                email = user.Email,
                                roles = roles
                            });
                        }
                    }
                }

                return Unauthorized(new { message = "Chưa đăng nhập" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy thông tin người dùng." });
            }
        }

        // Access Denied
        [HttpGet("access-denied")]
        public IActionResult AccessDenied()
        {
            return StatusCode(403, new { message = "Bạn không có quyền truy cập vào tài nguyên này." });
        }

    }
}

// POST: api/auth/login          → Đăng nhập JWT, trả về token
// POST: api/auth/login-cookie   → Đăng nhập Identity với Cookie
// POST: api/auth/secret-login   → Đăng nhập bí mật (cần key), trả về token
// POST: api/auth/logout         → Đăng xuất JWT, hủy token hiện tại
// POST: api/auth/logout-cookie  → Đăng xuất Cookie
// GET:  api/auth/check-auth     → Kiểm tra trạng thái đăng nhập
// GET:  api/auth/access-denied  → Trang access denied
