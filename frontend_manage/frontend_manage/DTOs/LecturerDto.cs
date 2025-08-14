using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs
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
    
    
    public class LecturerExamRoomDto
    {
        public int ExamRoomLecturerAssignmentId { get; set; }
        public int ExamRoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int ExamSessionSubjectId { get; set; }
        public string ExamSessionName { get; set; } = string.Empty;
        public string LecturerName { get; set; } = string.Empty;
        public string LecturerCode { get; set; } = string.Empty;
        public DateTime? ExamStartTime { get; set; }
        public DateTime? ExamEndTime { get; set; }
        public string ExamStatus { get; set; } = string.Empty; // "pending", "ongoing", "completed"
        public int StudentCount { get; set; }
        public DateTime StartTime => ExamStartTime ?? DateTime.Now;
        public string Status => ExamStatus;
    }
    
    public class LecturerLoginDto
    {
        public string LecturerCode1 { get; set; } = string.Empty;
        public string LecturerCode2 { get; set; } = string.Empty;
    }

    public class LecturerAuthResultDto
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

}
