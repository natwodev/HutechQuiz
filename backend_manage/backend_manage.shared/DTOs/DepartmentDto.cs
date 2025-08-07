namespace backend_manage.shared.DTOs
{
    public class DepartmentDto
    {
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }

    public class DepartmentCreateDto
    {
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }

    public class DepartmentUpdateDto
    {
        public string DepartmentName { get; set; }
    }
} 