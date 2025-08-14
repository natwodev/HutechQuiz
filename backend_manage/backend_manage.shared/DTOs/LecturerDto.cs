namespace backend_manage.shared.DTOs
{
    public class LecturerCreateDto
    {
        public string LecturerCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string DepartmentId { get; set; }
    }

    public class LecturerDto
    {
        public int LecturerId { get; set; }
        public string LecturerCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? Gender { get; set; }
        public System.DateTime? DateOfBirth { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }

    public class LecturerAuthResultDto
    {
        public string Token { get; set; }
        public string Role { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class LecturerLoginDto
    {
        public string LecturerCode1 { get; set; }
        public string LecturerCode2 { get; set; }
    }
} 