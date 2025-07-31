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
using StackExchange.Redis.Profiling;

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
            
            // Cấu hình Redis với fallback khi không khả dụng
            services.AddSingleton<IConnectionMultiplexer>(sp => {
                var logger = sp.GetRequiredService<ILogger<RedisLogger>>();
                var redisConnectionString = configuration["Redis:ConnectionString"] ?? "localhost:6380,abortConnect=false";
                
                try
                {
                    var options = ConfigurationOptions.Parse(redisConnectionString);
                    options.ConnectRetry = 10;
                    options.ConnectTimeout = 30000;
                    options.SyncTimeout = 30000;
                    options.ResponseTimeout = 30000;
                    options.KeepAlive = 180;
                    options.AbortOnConnectFail = false;
                    options.ReconnectRetryPolicy = new ExponentialRetry(5);
                    options.ConfigCheckSeconds = 60;
                    
                    logger.LogInformation("Đang kết nối Redis với connection string: {ConnectionString}", redisConnectionString);
                    var redis = ConnectionMultiplexer.Connect(options);
                    
                    // Kiểm tra kết nối
                    if (redis.IsConnected)
                    {
                        logger.LogInformation("Kết nối Redis thành công!");
                        return redis;
                    }
                    else
                    {
                        logger.LogWarning("Redis không khả dụng, sẽ sử dụng fallback");
                        return CreateRedisFallback(logger);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Không thể kết nối Redis, sẽ sử dụng fallback");
                    return CreateRedisFallback(logger);
                }
            });

            //đăng ký tạo policy phân quyền
            services.AddAuthorization(options =>
            {
                AuthorizationPolicies.AddCustomPolicies(options);
            });
            
            services.AddMemoryCache();
            // services.Configure<IpRateLimitOptions>(configuration.GetSection("RateLimit")); // Tắt rate limiting
            // services.AddInMemoryRateLimiting(); // Tắt rate limiting
            // services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>(); // Tắt rate limiting
        }
        
        private static IConnectionMultiplexer CreateRedisFallback(ILogger logger)
        {
            logger.LogWarning("Tạo Redis fallback - Redis không khả dụng, hệ thống sẽ chạy không có cache");
            
            // Tạo một connection multiplexer giả để không crash hệ thống
            // Các service sẽ fallback về database khi Redis không khả dụng
            var options = ConfigurationOptions.Parse("localhost:6379,abortConnect=false");
            options.ConnectRetry = 0; // Không retry
            options.ConnectTimeout = 1000; // Timeout nhanh
            options.SyncTimeout = 1000;
            options.ResponseTimeout = 1000;
            options.AbortOnConnectFail = false;
            
            return ConnectionMultiplexer.Connect(options);
        }
    }
}