namespace backend_manage.DTOs
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
        public string? UserId { get; set; }
    }
} 