using System.Text;
using System.Linq;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace backend_manage.core.Middlewares.Jwt
{
    public static class AuthenticationConfig
    {
        public static void ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // Đồng bộ với appsettings.json (trường "JWT:key")
            var key = Encoding.ASCII.GetBytes(configuration["JWT:key"] ?? "");

            services.AddAuthentication(options =>
                {
                    // Sử dụng đúng scheme cookie của Identity để đồng bộ với SignInManager
                    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultSignOutScheme = IdentityConstants.ApplicationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidIssuer = configuration["JWT:Issuer"],
                        ValidAudience = configuration["JWT:Audience"]
                    };
                });

            // Cấu hình cookie từ appsettings
            var cookieConfig = configuration.GetSection("Cookie");
            var cookieName = cookieConfig["Name"] ?? "HutechQuiz.Auth";
            var expireHours = int.Parse(cookieConfig["ExpireTimeSpanHours"] ?? "8");
            var slidingExpiration = bool.Parse(cookieConfig["SlidingExpiration"] ?? "true");
            var httpOnly = bool.Parse(cookieConfig["HttpOnly"] ?? "true");
            var sameSite = Enum.Parse<SameSiteMode>(cookieConfig["SameSite"] ?? "Lax");
            var securePolicy = Enum.Parse<CookieSecurePolicy>(cookieConfig["SecurePolicy"] ?? "None");
            
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/api/auth/login-cookie";
                options.LogoutPath = "/api/auth/logout-cookie";
                options.AccessDeniedPath = "/api/auth/access-denied";
                options.Cookie.Name = cookieName;
                options.Cookie.HttpOnly = httpOnly;
                options.Cookie.SameSite = sameSite;
                options.Cookie.SecurePolicy = securePolicy;
                
                options.ExpireTimeSpan = TimeSpan.FromHours(expireHours);
                options.SlidingExpiration = slidingExpiration;

                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    },
                    OnValidatePrincipal = context =>
                    {
                        // Log để debug cookie validation
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<CookieAuthenticationEvents>>();
                        
                        logger.LogInformation("🍪 Cookie validation started");
                        logger.LogInformation("🔍 Has Principal: {HasPrincipal}", context.Principal != null);
                        logger.LogInformation("🔍 Principal Identity: {IdentityName}", context.Principal?.Identity?.Name ?? "NULL");
                        logger.LogInformation("🔍 Is Authenticated: {IsAuthenticated}", context.Principal?.Identity?.IsAuthenticated ?? false);
                        logger.LogInformation("🔍 Claims Count: {ClaimsCount}", context.Principal?.Claims?.Count() ?? 0);
                        
                        if (context.Principal?.Identity?.IsAuthenticated == true)
                        {
                            var roles = context.Principal.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
                            logger.LogInformation("✅ Cookie validation successful for user: {UserName}, Roles: {Roles}", 
                                context.Principal.Identity.Name, string.Join(", ", roles));
                        }
                        else
                        {
                            logger.LogWarning("❌ Cookie validation failed - cookie may be expired or invalid");
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        }
    }
}