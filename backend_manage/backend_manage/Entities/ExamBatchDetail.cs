using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.Entities
{
    // Entity đại diện cho một lần thi trong một đợt thi
    public class ExamBatchDetail : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ExamBatchDetailId { get; set; }

        // Khóa ngoại liên kết với ExamBatch
        [ForeignKey("ExamBatch")]
        public int ExamBatchId { get; set; }
        public ExamBatch ExamBatch { get; set; }

        // Tên hoặc mô tả lần thi (ví dụ: Lần 1, Lần 2, Phúc khảo...)
        [StringLength(100)]
        public string Name { get; set; }

        // Navigation property: mỗi lần thi có nhiều ca thi
        public ICollection<ExamSession> ExamSessions { get; set; } = new List<ExamSession>();
    }
} 