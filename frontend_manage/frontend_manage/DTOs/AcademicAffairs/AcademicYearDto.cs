using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class AcademicYearDto
    {
        public int AcademicYearId { get; set; }
        public string YearName { get; set; } = string.Empty;
        public List<SemesterDto> Semesters { get; set; } = new();
    }

    public class AcademicYearCreateDto
    {
        [Required(ErrorMessage = "Tên năm học là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên năm học không được vượt quá 50 ký tự")]
        public string YearName { get; set; } = string.Empty;
    }

    public class AcademicYearUpdateDto
    {
        [Required(ErrorMessage = "Tên năm học là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên năm học không được vượt quá 50 ký tự")]
        public string YearName { get; set; } = string.Empty;
    }
}



