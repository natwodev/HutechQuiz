namespace backend_manage.shared.DTOs
{
    public class StudentDto
    {
       // public int StudentId { get; set; }
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        //public string Username { get; set; }
        public bool IsLogin { get; set; }
        public bool? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string DepartmentId { get; set; }
        public string? UserId { get; set; }
    }

    public class StudentCreateDto
    {
        public string StudentCode { get; set; }
        public string FirstName { get; set; }
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
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string DepartmentId { get; set; }
        public string? UserId { get; set; }
    }
} 
