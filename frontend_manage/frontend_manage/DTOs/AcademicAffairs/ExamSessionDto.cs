using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class ExamSessionDto
    {
        public int ExamSessionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
        public int ExamBatchDetailId { get; set; }
        public string ExamBatchDetailName { get; set; } = string.Empty;
    }

    public class ExamSessionCreateDto
    {
        [Required(ErrorMessage = "Tên ca thi là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên ca thi không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public DateTime EndTime { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsCompleted { get; set; } = false;

        [Required(ErrorMessage = "Chi tiết đợt thi là bắt buộc")]
        public int ExamBatchDetailId { get; set; }
    }

    public class ExamSessionUpdateDto
    {
        [Required(ErrorMessage = "Tên ca thi là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên ca thi không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public DateTime EndTime { get; set; }

        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }

        [Required(ErrorMessage = "Chi tiết đợt thi là bắt buộc")]
        public int ExamBatchDetailId { get; set; }
    }
}



