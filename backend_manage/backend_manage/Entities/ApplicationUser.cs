using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace backend_manage.Entities;

// Entity đại diện cho người dùng hệ thống (admin, giảng viên, etc.)
// Kế thừa từ IdentityUser của ASP.NET Core Identity
public class ApplicationUser : IdentityUser
{
    // Họ và tên đầy đủ của người dùng
    [MaxLength(100)]
    public string? FullName { get; set; }
    
    public Lecturer? Lecturer { get; set; }

}