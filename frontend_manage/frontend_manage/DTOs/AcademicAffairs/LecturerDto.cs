using System;
using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class LecturerDto
    {
        public int LecturerId { get; set; }
        public string LecturerCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string UserId { get; set; }
    }

    public class LecturerCreateDto
    {
        [Required(ErrorMessage = "Mã giảng viên không được để trống")]
        public string LecturerCode { get; set; }
        
        [Required(ErrorMessage = "Họ không được để trống")]
        public string FirstName { get; set; }
        
        [Required(ErrorMessage = "Tên không được để trống")]
        public string LastName { get; set; }
        
        public bool? Gender { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }
        
        public string PhoneNumber { get; set; }
        
        [Required(ErrorMessage = "Khoa không được để trống")]
        public string DepartmentId { get; set; }
    }

    public class LecturerUpdateDto
    {
        [Required(ErrorMessage = "Họ không được để trống")]
        public string FirstName { get; set; }
        
        [Required(ErrorMessage = "Tên không được để trống")]
        public string LastName { get; set; }
        
        public bool? Gender { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }
        
        public string PhoneNumber { get; set; }
        
        [Required(ErrorMessage = "Khoa không được để trống")]
        public string DepartmentId { get; set; }
    }
}
