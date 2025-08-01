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
            
            // Cấu hình Redis với kiểm tra kết nối ngay khi khởi động
            services.AddSingleton<IConnectionMultiplexer>(sp => {
                var logger = sp.GetRequiredService<ILogger<RedisLogger>>();
                var redisConnectionString = configuration["Redis:ConnectionString"] ?? "localhost:6380,abortConnect=false";
                
                logger.LogInformation("Đang kiểm tra kết nối Redis...");
                
                try
                {
                    var options = ConfigurationOptions.Parse(redisConnectionString);
                    options.ConnectRetry = 3; // Tăng retry lên 3 lần
                    options.ConnectTimeout = 10000; // Tăng timeout lên 10 giây
                    options.SyncTimeout = 10000;
                    options.ResponseTimeout = 10000;
                    options.KeepAlive = 60;
                    options.AbortOnConnectFail = false;
                    options.ReconnectRetryPolicy = new ExponentialRetry(5); // Tăng retry policy
                    options.ConfigCheckSeconds = 30;
                    options.AsyncTimeout = 10000;
                    
                    // Thêm cấu hình cho high concurrency
                    options.TieBreaker = "hutech_quiz_tiebreaker";
                    options.DefaultDatabase = 0;
                    options.ChannelPrefix = "hutech_quiz";
                    
                    logger.LogInformation("Đang kết nối Redis với connection string: {ConnectionString}", redisConnectionString);
                    var redis = ConnectionMultiplexer.Connect(options);
                    
                    // Kiểm tra kết nối ngay lập tức
                    var db = redis.GetDatabase();
                    var pingResult = db.Ping();
                    
                    if (pingResult.TotalMilliseconds < 15000) // Tăng timeout lên 15 giây
                    {
                        logger.LogInformation("✅ Kết nối Redis thành công! Ping time: {PingTime}ms", pingResult.TotalMilliseconds);
                        return redis;
                    }
                    else
                    {
                        logger.LogWarning("⚠️ Redis ping quá chậm ({PingTime}ms), sẽ sử dụng fallback", pingResult.TotalMilliseconds);
                        return CreateRedisFallback(logger, redisConnectionString);
                    }
                }
                catch (RedisConnectionException ex)
                {
                    logger.LogWarning("❌ Không thể kết nối Redis: {Message}", ex.Message);
                    return CreateRedisFallback(logger, redisConnectionString);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("❌ Lỗi khi kết nối Redis: {Message}", ex.Message);
                    return CreateRedisFallback(logger, redisConnectionString);
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
        
        private static IConnectionMultiplexer CreateRedisFallback(ILogger logger, string connectionString)
        {
            logger.LogWarning("🔄 Tạo Redis fallback - Redis không khả dụng, hệ thống sẽ chạy không có cache");
            
            // Tạo một connection multiplexer với cấu hình cho phép kết nối lại
            var options = ConfigurationOptions.Parse(connectionString);
            options.ConnectRetry = 0; // Không retry ngay lập tức
            options.ConnectTimeout = 100; // Timeout rất nhanh
            options.SyncTimeout = 100;
            options.ResponseTimeout = 100;
            options.AbortOnConnectFail = false;
            options.KeepAlive = 0; // Tắt keep-alive
            options.ReconnectRetryPolicy = new ExponentialRetry(5); // Cho phép reconnect sau này
            
            try
            {
                var redis = ConnectionMultiplexer.Connect(options);
                logger.LogInformation("✅ Redis fallback đã được tạo thành công - sẽ tự động kết nối lại khi Redis khả dụng");
                return redis;
            }
            catch (Exception ex)
            {
                logger.LogWarning("⚠️ Không thể tạo Redis fallback ngay lập tức: {Message}", ex.Message);
                
                // Thử tạo một connection đơn giản hơn
                try
                {
                    var simpleOptions = ConfigurationOptions.Parse("localhost:6380,abortConnect=false");
                    simpleOptions.ConnectRetry = 0;
                    simpleOptions.ConnectTimeout = 50;
                    simpleOptions.SyncTimeout = 50;
                    simpleOptions.ResponseTimeout = 50;
                    simpleOptions.AbortOnConnectFail = false;
                    
                    var simpleRedis = ConnectionMultiplexer.Connect(simpleOptions);
                    logger.LogInformation("✅ Redis fallback đơn giản đã được tạo thành công");
                    return simpleRedis;
                }
                catch (Exception simpleEx)
                {
                    logger.LogError(simpleEx, "❌ Không thể tạo Redis fallback, hệ thống sẽ chạy không có cache");
                    throw new InvalidOperationException("Redis không khả dụng và không thể tạo fallback", simpleEx);
                }
            }
        }
    }
}