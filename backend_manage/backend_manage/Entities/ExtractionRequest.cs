using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.Entities
{
    // Entity đại diện cho yêu cầu đề thi trong hệ thống
    // Kế thừa từ BaseEntity để có các trường audit
    public class ExamPaperRequest : BaseEntity
    {
        // Khóa chính của bảng ExamPaperRequest (tự động tăng)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ExamPaperRequestId { get; set; }

        // Ngày lấy dữ liệu
        public DateTime? ExtractionDate { get; set; }

        // Mã giáo viên
        [StringLength(50)]
        public string TeacherCode { get; set; }

        // Tên giáo viên
        [StringLength(100)]
        public string TeacherName { get; set; }

        // Khóa ngoại liên kết với bảng ShuffledExamPaper
        [ForeignKey("ShuffledExamPaper")]
        public int ShuffledExamPaperId { get; set; }
        
        // Navigation property đến entity ShuffledExamPaper
        public ShuffledExamPaper ShuffledExamPaper { get; set; }
    }
} 