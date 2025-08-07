using Microsoft.AspNetCore.Authorization;

namespace backend_manage.core.Configurations
{
    public static class AuthorizationPolicies
    {

        public static void AddCustomPolicies(AuthorizationOptions options)
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));
            
            options.AddPolicy("StudentOnly", policy =>
                policy.RequireRole("Student"));
            
            options.AddPolicy("LecturerOnly", policy =>
                policy.RequireRole("Lecturer"));
            
            options.AddPolicy("StudentOrAdmin", policy =>
                policy.RequireRole("Student","Admin"));
            
            options.AddPolicy("LecturerOrAdmin", policy =>
                policy.RequireRole("Lecturer","Admin"));

            options.AddPolicy("AdminStudentLecturer", policy =>
                policy.RequireRole("Admin", "Student", "Lecturer"));
        }
    }
}