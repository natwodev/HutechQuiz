using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class SubjectDto
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class SubjectCreateDto
    {
        [Required(ErrorMessage = "Mã môn học là bắt buộc")]
        [StringLength(20, ErrorMessage = "Mã môn học không được vượt quá 20 ký tự")]
        public string SubjectCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên môn học là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên môn học không được vượt quá 100 ký tự")]
        public string SubjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Khoa là bắt buộc")]
        public int DepartmentId { get; set; }
    }

    public class SubjectUpdateDto
    {
        [Required(ErrorMessage = "Mã môn học là bắt buộc")]
        [StringLength(20, ErrorMessage = "Mã môn học không được vượt quá 20 ký tự")]
        public string SubjectCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên môn học là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên môn học không được vượt quá 100 ký tự")]
        public string SubjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Khoa là bắt buộc")]
        public int DepartmentId { get; set; }
    }
}



