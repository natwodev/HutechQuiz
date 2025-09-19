using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class ExamBatchDetailDto
    {
        public int ExamBatchDetailId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ExamBatchId { get; set; }
        public string ExamBatchName { get; set; } = string.Empty;
        public List<ExamSessionDto> ExamSessions { get; set; } = new();
    }

    public class ExamBatchDetailCreateDto
    {
        [Required(ErrorMessage = "Tên chi tiết đợt thi là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên chi tiết đợt thi không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Đợt thi là bắt buộc")]
        public int ExamBatchId { get; set; }

        public List<ExamSessionCreateDto> ExamSessions { get; set; } = new();
    }

    public class ExamBatchDetailUpdateDto
    {
        [Required(ErrorMessage = "Tên chi tiết đợt thi là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên chi tiết đợt thi không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Đợt thi là bắt buộc")]
        public int ExamBatchId { get; set; }

        public List<ExamSessionUpdateDto> ExamSessions { get; set; } = new();
    }
}



