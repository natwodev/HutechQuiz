using System.Security.Claims;
using backend_manage.core.Authentication.Services;
using backend_manage.core.Jwt;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
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
        private readonly ILecturerService _lecturerService;

        
        public AuthController(IAuthService authService, IConfiguration configuration, IUserService userService, ILecturerService lecturerService)
        {
            _authService = authService;
            _configuration = configuration;
            _userService = userService;
            _lecturerService = lecturerService;
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
        
        
        // Đăng nhập cho giảng viên
        [HttpPost("lecturer-login")]
        public async Task<IActionResult> LecturerLogin([FromBody] LecturerLoginDto loginRequest)
        {
            try
            {
                var authResult = await _lecturerService.LoginAsync(loginRequest.LecturerCode1, loginRequest.LecturerCode2);

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

    }
}

// POST: api/auth/login          → Đăng nhập JWT, trả về token
// POST: api/auth/logout         → Đăng xuất JWT, hủy token hiện tại
// GET:  api/auth/check-auth     → Kiểm tra trạng thái đăng nhập
// GET:  api/auth/access-denied  → Trang access denied
// POST: api/auth/lecturer-login → Đăng nhập cho giảng viên, trả về token
