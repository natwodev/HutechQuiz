using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using backend_manage.Configurations;
using backend_manage.Data;
using backend_manage.Entities;
using backend_manage.Mappings;
using backend_manage.Middlewares.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace backend_manage.Extensions
{
    public class RedisLogger { }
    
    public static class ServiceExtensions
    {
        public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            /*
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddGoogle(googleOptions =>
                {
                    IConfigurationSection googleAuthNSection = configuration.GetSection("Authentication:Google");

                    googleOptions.ClientId = googleAuthNSection["ClientId"];
                    googleOptions.ClientSecret = googleAuthNSection["ClientSecret"];

                    // Optional: cấu hình đường callback nếu bạn dùng route tùy chỉnh
                    // googleOptions.CallbackPath = new PathString("/api/auth/external-login-callback");
                });
                */
            services.AddControllers().AddJsonOptions(x =>
                x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
            
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            
            services.AddSignalR();
            
            services.Configure<IdentityOptions>(options =>
            {
                // Password
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 3;
                options.Password.RequiredUniqueChars = 0;

                // Lockout
                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            });

            services.AddAutoMapper(typeof(MappingProfile));

            
            // Đăng ký DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Đăng ký CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            });


            // Đăng ký các service khác
            services.ConfigureDependencies();

            // Đăng ký xác thực JWT (được tách riêng)
            services.ConfigureJwt(configuration);
            
            // Cấu hình Redis với logging
            services.AddSingleton<IConnectionMultiplexer>(sp => {
                var logger = sp.GetRequiredService<ILogger<RedisLogger>>();
                var connectionString = configuration["Redis:ConnectionString"] ?? "localhost:6379";
                return sp.ConfigureRedis(connectionString, logger);
            });

            //đăng ký tạo policy phân quyền
            services.AddAuthorization(options =>
            {
                AuthorizationPolicies.AddCustomPolicies(options);
            });
            
            services.AddMemoryCache();
            services.Configure<IpRateLimitOptions>(configuration.GetSection("RateLimit"));
            services.AddInMemoryRateLimiting();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        }
    }
}