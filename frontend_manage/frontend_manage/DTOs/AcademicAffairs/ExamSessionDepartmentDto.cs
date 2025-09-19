using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class ExamSessionDepartmentDto
    {
        public int ExamSessionDepartmentId { get; set; }
        public int ExamSessionId { get; set; }
        public string ExamSessionName { get; set; } = string.Empty;
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class ExamSessionDepartmentCreateDto
    {
        [Required(ErrorMessage = "Ca thi là bắt buộc")]
        public int ExamSessionId { get; set; }

        [Required(ErrorMessage = "Khoa là bắt buộc")]
        public string DepartmentId { get; set; } = string.Empty;
    }

    public class ExamSessionDepartmentUpdateDto
    {
        [Required(ErrorMessage = "Ca thi là bắt buộc")]
        public int ExamSessionId { get; set; }

        [Required(ErrorMessage = "Khoa là bắt buộc")]
        public string DepartmentId { get; set; } = string.Empty;
    }
}



