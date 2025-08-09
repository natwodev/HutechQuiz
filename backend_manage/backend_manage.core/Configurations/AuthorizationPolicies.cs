using Microsoft.AspNetCore.Authorization;

namespace backend_manage.core.Configurations
{
    public static class AuthorizationPolicies
    {
        /// <summary>
        /// Thêm các policy phân quyền tùy chỉnh dựa trên các role trong seed data
        /// </summary>
        /// <param name="options">AuthorizationOptions để cấu hình policies</param>
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

            // New policies based on seed data roles
            /// <summary>
            /// Chỉ cho phép Phòng đào tạo truy cập
            /// </summary>
            options.AddPolicy("AcademicAffairsOnly", policy =>
                policy.RequireRole("AcademicAffairs"));
            
            /// <summary>
            /// Chỉ cho phép Quản lý đợt thi truy cập
            /// </summary>
            options.AddPolicy("ExamManagerOnly", policy =>
                policy.RequireRole("ExamManager"));
            
            /// <summary>
            /// Chỉ cho phép Quản trị CNTT truy cập
            /// </summary>
            options.AddPolicy("ITManagerOnly", policy =>
                policy.RequireRole("ITManager"));

            // Combined policies for academic management
            /// <summary>
            /// Cho phép Admin, Phòng đào tạo, và Quản lý đợt thi truy cập
            /// Dùng cho các chức năng quản lý học tập
            /// </summary>
            options.AddPolicy("AcademicManagement", policy =>
                policy.RequireRole("Admin", "AcademicAffairs", "ExamManager"));
            
            /// <summary>
            /// Cho phép Admin và Phòng đào tạo truy cập
            /// Dùng cho các chức năng quản lý năm học
            /// </summary>
            options.AddPolicy("AcademicAffairsOrAdmin", policy =>
                policy.RequireRole("Admin", "AcademicAffairs"));
            
            /// <summary>
            /// Cho phép Admin và Quản lý đợt thi truy cập
            /// Dùng cho các chức năng quản lý thi cử
            /// </summary>
            options.AddPolicy("ExamManagement", policy =>
                policy.RequireRole("Admin", "ExamManager"));
            
            /// <summary>
            /// Cho phép Admin và Quản trị CNTT truy cập
            /// Dùng cho các chức năng quản trị hệ thống
            /// </summary>
            options.AddPolicy("ITManagement", policy =>
                policy.RequireRole("Admin", "ITManager"));
            
            /// <summary>
            /// Cho phép tất cả nhân viên truy cập (không bao gồm sinh viên)
            /// Dùng cho các chức năng nội bộ
            /// </summary>
            options.AddPolicy("StaffOnly", policy =>
                policy.RequireRole("Admin", "Lecturer", "AcademicAffairs", "ExamManager", "ITManager"));
            
            /// <summary>
            /// Cho phép quản lý truy cập (không bao gồm giảng viên và sinh viên)
            /// Dùng cho các chức năng quản lý cấp cao
            /// </summary>
            options.AddPolicy("ManagementOnly", policy =>
                policy.RequireRole("Admin", "AcademicAffairs", "ExamManager", "ITManager"));
            
            /// <summary>
            /// Cho phép Admin, Giảng viên, và Phòng đào tạo truy cập
            /// Dùng cho các chức năng liên quan đến giảng dạy
            /// </summary>
            options.AddPolicy("AcademicStaff", policy =>
                policy.RequireRole("Admin", "Lecturer", "AcademicAffairs"));
            
            /// <summary>
            /// Cho phép tất cả người dùng truy cập
            /// Dùng cho các chức năng công khai
            /// </summary>
            options.AddPolicy("AllUsers", policy =>
                policy.RequireRole("Admin", "Student", "Lecturer", "AcademicAffairs", "ExamManager", "ITManager"));
        }
    }
}