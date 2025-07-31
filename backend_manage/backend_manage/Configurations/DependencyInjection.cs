using backend_manage.Authentication.Repositories;
using backend_manage.Authentication.Services;
using backend_manage.Repositories.AuthRepository;
using backend_manage.Repositories.Interfaces;
using backend_manage.Services.AuthService;
using backend_manage.Services.AuthService.Helpers;
using backend_manage.Services.Interfaces;
using backend_manage.Services;
using backend_manage.Messages.RabbitMQ;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace backend_manage.Configurations
{
    public static class DependencyInjection
    {
        public static void ConfigureDependencies(this IServiceCollection services)
        {
        
            // Register Repositories
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // Register Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IAcademicYearService, AcademicYearService>();
            services.AddScoped<ISemesterService, SemesterService>();
            services.AddScoped<IExamBatchService, ExamBatchService>();
            services.AddScoped<IExamBatchDetailService, ExamBatchDetailService>();
            services.AddScoped<IExamSessionService, ExamSessionService>();
            services.AddScoped<IExamSessionDepartmentService, ExamSessionDepartmentService>();
            services.AddScoped<IExamSessionSubjectService, ExamSessionSubjectService>();
            services.AddScoped<IOriginalExamPaperService, OriginalExamPaperService>();
            services.AddScoped<IShuffledExamPaperService, ShuffledExamPaperService>();
            services.AddScoped<IExamRoomLecturerAssignmentService, ExamRoomLecturerAssignmentService>();
            services.AddScoped<ILecturerService, LecturerService>();
            
            // Register Helper Classes
            services.AddScoped<StudentCacheHelper>();
            services.AddScoped<StudentExamSessionCacheHelper>();
            services.AddScoped<ExamPaperHelper>();
            services.AddScoped<StudentAnswerHelper>();
            services.AddScoped<StudentValidationHelper>();
            services.AddScoped<StudentImportHelper>();

            // Register Redis Service as Singleton to avoid ping on every request
            services.AddSingleton<IRedisService>(sp =>
            {
                try
                {
                    // Sử dụng IConnectionMultiplexer đã được đăng ký trong ServiceExtensions
                    var redis = sp.GetRequiredService<IConnectionMultiplexer>();
                    
                    // Kiểm tra kết nối Redis chỉ một lần khi khởi tạo
                    if (redis.IsConnected)
                    {
                        var db = redis.GetDatabase();
                        var pingResult = db.Ping();
                        
                        if (pingResult.TotalMilliseconds < 5000)
                        {
                            var logger = sp.GetRequiredService<ILogger<RedisService>>();
                            logger.LogInformation("✅ Redis service đã được khởi tạo thành công - Ping: {PingTime}ms", pingResult.TotalMilliseconds);
                            return new RedisService(redis, logger);
                        }
                        else
                        {
                            var logger = sp.GetRequiredService<ILogger<RedisFallbackService>>();
                            logger.LogWarning("⚠️ Redis ping chậm ({PingTime}ms), sử dụng fallback", pingResult.TotalMilliseconds);
                            return new RedisFallbackService(logger);
                        }
                    }
                    else
                    {
                        var logger = sp.GetRequiredService<ILogger<RedisFallbackService>>();
                        logger.LogWarning("⚠️ Redis không kết nối, sử dụng fallback");
                        return new RedisFallbackService(logger);
                    }
                }
                catch (RedisConnectionException ex)
                {
                    var logger = sp.GetRequiredService<ILogger<RedisFallbackService>>();
                    logger.LogWarning("❌ Redis connection exception: {Message}", ex.Message);
                    return new RedisFallbackService(logger);
                }
                catch (Exception ex)
                {
                    var logger = sp.GetRequiredService<ILogger<RedisFallbackService>>();
                    logger.LogWarning("❌ Không thể khởi tạo Redis service: {Message}", ex.Message);
                    return new RedisFallbackService(logger);
                }
            });
            
            // Register RabbitMQ services with fallback
            services.AddSingleton<IRabbitMqService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<RabbitMqService>>();
                var configuration = sp.GetRequiredService<IConfiguration>();
                
                try
                {
                    var rabbitMqService = new RabbitMqService(configuration, logger);
                    logger.LogInformation("RabbitMQ service đã được khởi tạo thành công");
                    return rabbitMqService;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Không thể khởi tạo RabbitMQ service, sẽ sử dụng fallback");
                    return new RabbitMqFallbackService(logger);
                }
            });
            
            services.AddSingleton<IRabbitMqConsumer>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<RabbitMqConsumer>>();
                var rabbitMqService = sp.GetRequiredService<IRabbitMqService>();
                var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                
                try
                {
                    var consumer = new RabbitMqConsumer(rabbitMqService, serviceScopeFactory, logger);
                    logger.LogInformation("RabbitMQ consumer đã được khởi tạo thành công");
                    return consumer;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Không thể khởi tạo RabbitMQ consumer, sẽ sử dụng fallback");
                    return new RabbitMqFallbackConsumer(logger);
                }
            });
            
        }
    }
}