using backend_manage.core.Authentication.Repositories;
using backend_manage.core.Authentication.Services;
using backend_manage.core.Messages.RabbitMQ;
using backend_manage.core.Repositories.AuthRepository;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.AuthService;
using backend_manage.core.Services.AuthService.Helpers;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace backend_manage.core.Configurations
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
            services.AddScoped<StudentImportHelper>();

            // Register Message Processing Service
            services.AddSingleton<IMessageProcessingService, MessageProcessingService>();

            // Register Redis Service as Singleton to avoid ping on every request
            services.AddSingleton<IRedisService>(sp =>
            {
                try
                {
                    // Sử dụng IConnectionMultiplexer đã được đăng ký trong ServiceExtensions
                    var redis = sp.GetRequiredService<IConnectionMultiplexer>();
                    var logger = sp.GetRequiredService<ILogger<RedisService>>();
                    
                    // Kiểm tra kết nối Redis chỉ một lần khi khởi tạo
                    if (redis.IsConnected)
                    {
                        var db = redis.GetDatabase();
                        var pingResult = db.Ping();
                        
                        if (pingResult.TotalMilliseconds < 5000)
                        {
                            logger.LogInformation("✅ Redis service đã được khởi tạo thành công - Ping: {PingTime}ms", pingResult.TotalMilliseconds);
                        }
                        else
                        {
                            logger.LogWarning("⚠️ Redis ping chậm ({PingTime}ms), nhưng vẫn sử dụng RedisService", pingResult.TotalMilliseconds);
                        }
                    }
                    else
                    {
                        logger.LogWarning("⚠️ Redis không kết nối, nhưng vẫn sử dụng RedisService");
                    }
                    
                    return new RedisService(redis, logger);
                }
                catch (RedisConnectionException ex)
                {
                    var logger = sp.GetRequiredService<ILogger<RedisService>>();
                    logger.LogWarning("❌ Redis connection exception: {Message}", ex.Message);
                    return new RedisService(sp.GetRequiredService<IConnectionMultiplexer>(), logger);
                }
                catch (Exception ex)
                {
                    var logger = sp.GetRequiredService<ILogger<RedisService>>();
                    logger.LogWarning("❌ Không thể khởi tạo Redis service: {Message}", ex.Message);
                    return new RedisService(sp.GetRequiredService<IConnectionMultiplexer>(), logger);
                }
            });

            // Register RabbitMQ Services
            services.AddHostedService<RabbitMqReconnectWorker>();
            services.AddSingleton<IRabbitMqService, RabbitMqService>();
            services.AddHostedService<RabbitMqConsumer>();

        }
    }
}