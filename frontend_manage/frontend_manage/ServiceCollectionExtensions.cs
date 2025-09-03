using Microsoft.Extensions.DependencyInjection;
using frontend_manage.Services;
using frontend_manage.Services.AcademicAffairs;
using frontend_manage.Pages.Admin;
using frontend_manage.Pages.StudentLogin;
using frontend_manage.Pages.Exam;
using frontend_manage.Pages.Monitor;
using MudBlazor.Services;
using MudBlazor;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace frontend_manage
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Add MudBlazor services
            services.AddMudServices();

            // Core services
            services.AddScoped<AuthService>();
            services.AddScoped<StudentService>();
            services.AddScoped<MonitorService>();
            services.AddSingleton<NotificationService>();
            services.AddScoped<Api>();

            // Academic Affairs Services
            services.AddScoped<AcademicYearService>();
            services.AddScoped<SemesterService>();
            services.AddScoped<ExamBatchService>();
            services.AddScoped<ExamBatchDetailService>();
            services.AddScoped<ExamSessionService>();
            services.AddScoped<ExamSessionSubjectService>();

            // MathJax service
            services.AddScoped<IMathJaxService, MathJaxService>();

            // AuthHeaderHandler
            services.AddScoped<AuthHeaderHandler>();

            // InjectedServices - composition of common services
            services.AddScoped<InjectedServices>(sp => new InjectedServices
            {
                Navigation = sp.GetRequiredService<NavigationManager>(),
                StudentService = sp.GetRequiredService<StudentService>(),
                Snackbar = sp.GetRequiredService<ISnackbar>(),
                Dialog = sp.GetRequiredService<IDialogService>(),
                Http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API"),
                JSRuntime = sp.GetRequiredService<IJSRuntime>(),
                MathJaxService = sp.GetRequiredService<IMathJaxService>()
            });

            return services;
        }
    }
}
