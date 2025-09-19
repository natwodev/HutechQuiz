using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class ExamBatchDto
    {
        public int ExamBatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<ExamBatchDetailDto> ExamBatchDetails { get; set; } = new();
    }

    public class ExamBatchCreateDto
    {
        [Required(ErrorMessage = "Tên đợt thi là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên đợt thi không được vượt quá 100 ký tự")]
        public string BatchName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Học kỳ là bắt buộc")]
        public int SemesterId { get; set; }

        public bool IsActive { get; set; } = true;

        public List<ExamBatchDetailCreateDto> ExamBatchDetails { get; set; } = new();
    }

    public class ExamBatchUpdateDto
    {
        [Required(ErrorMessage = "Tên đợt thi là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên đợt thi không được vượt quá 100 ký tự")]
        public string BatchName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Học kỳ là bắt buộc")]
        public int SemesterId { get; set; }

        public bool IsActive { get; set; }

        public List<ExamBatchDetailUpdateDto> ExamBatchDetails { get; set; } = new();
    }
}



