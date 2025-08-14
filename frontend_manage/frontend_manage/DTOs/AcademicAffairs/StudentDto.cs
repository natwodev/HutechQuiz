using System;
using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class StudentDto
    {
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsLogin { get; set; }
        public bool? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string DepartmentId { get; set; }
        public string UserId { get; set; }
    }

    public class StudentCreateDto
    {
        [Required(ErrorMessage = "Mã sinh viên không được để trống")]
        public string StudentCode { get; set; }
        
        [Required(ErrorMessage = "Họ không được để trống")]
        public string FirstName { get; set; }
        
        [Required(ErrorMessage = "Tên không được để trống")]
        public string LastName { get; set; }
    }

    public class StudentUpdateDto
    {
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string DepartmentId { get; set; }
        public string UserId { get; set; }
    }

    public class AddExtraMinutesDto
    {
        [Required(ErrorMessage = "Mã sinh viên không được để trống")]
        public string StudentCode { get; set; }

        [Required(ErrorMessage = "ID phiên thi sinh viên không được để trống")]
        public int StudentExamSessionId { get; set; }

        [Required(ErrorMessage = "Số phút gia hạn không được để trống")]
        [Range(1, 60, ErrorMessage = "Số phút gia hạn phải từ 1-60 phút")]
        public int ExtraMinutes { get; set; }

        public string ReasonForExtra { get; set; }
    }

    public class SaveAnswerDto
    {
        [Required]
        public int StudentExamSessionId { get; set; }
        
        [Required]
        public int Index { get; set; }
        
        public int? SubIndex { get; set; }
        
        [Required]
        public string Answer { get; set; }
    }
}
