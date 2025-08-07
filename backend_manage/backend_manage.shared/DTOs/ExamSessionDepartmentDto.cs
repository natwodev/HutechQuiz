namespace backend_manage.shared.DTOs
{
    public class ExamSessionDepartmentDto
    {
        public int ExamSessionDepartmentId { get; set; }
        public int ExamSessionId { get; set; }
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string ExamSessionName { get; set; }
      
    }

    public class ExamSessionDepartmentCreateDto
    {
        public int ExamSessionId { get; set; }
        public string DepartmentId { get; set; }
    }

    public class ExamSessionDepartmentUpdateDto
    {
        public int ExamSessionId { get; set; }
        public string DepartmentId { get; set; }
    }
} 