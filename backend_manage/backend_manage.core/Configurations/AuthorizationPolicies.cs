using Microsoft.AspNetCore.Authorization;

namespace backend_manage.core.Configurations
{
    public static class AuthorizationPolicies
    {

        public static void AddCustomPolicies(AuthorizationOptions options)
        {
            // Existing policies
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

            options.AddPolicy("AcademicAffairsOnly", policy =>
                policy.RequireRole("AcademicAffairs"));
            

            options.AddPolicy("ExamManagerOnly", policy =>
                policy.RequireRole("ExamManager"));
            

            options.AddPolicy("ITManagerOnly", policy =>
                policy.RequireRole("ITManager"));


            options.AddPolicy("AcademicManagement", policy =>
                policy.RequireRole("Admin", "AcademicAffairs", "ExamManager"));
            
         
            options.AddPolicy("AcademicAffairsOrAdmin", policy =>
                policy.RequireRole("Admin", "AcademicAffairs"));
            
          
            options.AddPolicy("ExamManagement", policy =>
                policy.RequireRole("Admin", "ExamManager"));
            
            
            options.AddPolicy("ITManagement", policy =>
                policy.RequireRole("Admin", "ITManager"));
            
           
            options.AddPolicy("StaffOnly", policy =>
                policy.RequireRole("Admin", "Lecturer", "AcademicAffairs", "ExamManager", "ITManager"));
            
           
            options.AddPolicy("ManagementOnly", policy =>
                policy.RequireRole("Admin", "AcademicAffairs", "ExamManager", "ITManager"));
            
            
            options.AddPolicy("AcademicStaff", policy =>
                policy.RequireRole("Admin", "Lecturer", "AcademicAffairs"));
            
          
            options.AddPolicy("AllUsers", policy =>
                policy.RequireRole("Admin", "Student", "Lecturer", "AcademicAffairs", "ExamManager", "ITManager"));
        }
    }
}