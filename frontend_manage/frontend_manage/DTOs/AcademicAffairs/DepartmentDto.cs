using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class DepartmentDto
    {
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class DepartmentCreateDto
    {
        [Required(ErrorMessage = "Mã khoa là bắt buộc")]
        [StringLength(10, ErrorMessage = "Mã khoa không được vượt quá 10 ký tự")]
        public string DepartmentId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên khoa là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên khoa không được vượt quá 100 ký tự")]
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class DepartmentUpdateDto
    {
        [Required(ErrorMessage = "Tên khoa là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên khoa không được vượt quá 100 ký tự")]
        public string DepartmentName { get; set; } = string.Empty;
    }
}



