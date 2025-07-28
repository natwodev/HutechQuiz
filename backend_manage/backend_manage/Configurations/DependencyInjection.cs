using backend_manage.Authentication.Repositories;
using backend_manage.Authentication.Services;
using backend_manage.Repositories.AuthRepository;
using backend_manage.Repositories.Interfaces;
using backend_manage.Services.AuthService;
using backend_manage.Services.Interfaces;
using backend_manage.Messages.RabbitMQ;

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
            services.AddSingleton<IRabbitMQService, RabbitMQService>();
            services.AddSingleton<RabbitMQConsumer>();
        }
    }
}