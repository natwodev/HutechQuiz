using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class SemesterDto
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public int AcademicYearId { get; set; }
        public string AcademicYearName { get; set; } = string.Empty;
    }

    public class SemesterCreateDto
    {
        [Required(ErrorMessage = "Tên học kỳ là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên học kỳ không được vượt quá 50 ký tự")]
        public string SemesterName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Năm học là bắt buộc")]
        public int AcademicYearId { get; set; }
    }

    public class SemesterUpdateDto
    {
        [Required(ErrorMessage = "Tên học kỳ là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên học kỳ không được vượt quá 50 ký tự")]
        public string SemesterName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Năm học là bắt buộc")]
        public int AcademicYearId { get; set; }
    }
}



