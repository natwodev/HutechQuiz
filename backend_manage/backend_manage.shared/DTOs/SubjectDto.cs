namespace backend_manage.shared.DTOs
{
    public class SubjectDto
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public int DepartmentId { get; set; }
    }

    public class SubjectCreateDto
    {
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public int DepartmentId { get; set; }
    }

    public class SubjectUpdateDto
    {
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public int DepartmentId { get; set; }
    }
} 