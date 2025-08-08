using System.Security.Claims;
using backend_manage.core.Authentication.Services;
using backend_manage.core.Middlewares.Jwt;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using backend_manage.shared.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

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

        
        public AuthController(IAuthService authService, IConfiguration configuration, IUserService userService)
        {
            _authService = authService;
            _configuration = configuration;
            _userService = userService;
            _jwtTokenGenerator = new JwtTokenGenerator(configuration);
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
                
                // Lấy roles và permissions
                var roles = await _authService.GetUserRolesAsync(user);
                var permissions = await _authService.GetUserPermissionsAsync(user);

                return Ok(new
                {
                    message = "Đăng nhập thành công",
                    user = new
                    {
                        id = user.Id,
                        username = user.UserName,
                        email = user.Email,
                        roles = roles,
                        permissions = permissions
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
        
        // Đăng nhập bí mật
        [HttpPost("secret-login")]
        public async Task<IActionResult> SecretLogin([FromQuery] string key, [FromBody] LoginModelDto loginModel)
        {
            var secretKey = _configuration["SecretAccess:SecretLoginKey"];

            if (key != secretKey)
            {
                return Forbid("Bạn không có quyền sử dụng đường dẫn này.");
            }

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
            catch
            {
                return StatusCode(500, new { message = "Đăng nhập thất bại. Vui lòng thử lại sau!" });
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
                            var permissions = await _authService.GetUserPermissionsAsync(user);

                            return Ok(new
                            {
                                isAuthenticated = true,
                                user = new
                                {
                                    id = user.Id,
                                    username = user.UserName,
                                    email = user.Email,
                                    roles = roles,
                                    permissions = permissions
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
