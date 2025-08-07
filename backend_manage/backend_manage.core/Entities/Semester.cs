using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.core.Entities
{
    // Entity đại diện cho học kỳ trong hệ thống
    public class Semester : BaseEntity
    {
        // Khóa chính của bảng Semester (tự động tăng)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SemesterId { get; set; }

        // Tên học kỳ (ví dụ: Học kỳ 1, Học kỳ 2, Học kỳ hè)
        [MaxLength(100)]
        public string SemesterName { get; set; }
        
        // Khóa ngoại liên kết với bảng AcademicYear
        // Xác định học kỳ thuộc năm học nào
        [ForeignKey("AcademicYear")]
        public int AcademicYearId { get; set; }

        // Navigation property đến entity AcademicYear
        public AcademicYear AcademicYear { get; set; }
        
        // Collection các đợt thi thuộc học kỳ này
        // Mối quan hệ one-to-many với ExamBatch
        public ICollection<ExamBatch> ExamBatches { get; set; } = new List<ExamBatch>();
    }
}