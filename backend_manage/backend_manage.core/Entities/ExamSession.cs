using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.core.Entities;

// Entity đại diện cho ca thi trong hệ thống
// Kế thừa từ TimeRangeEntity để có các trường audit
public class ExamSession : BaseEntity
{
    // Khóa chính của bảng ExamSession (tự động tăng)
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ExamSessionId { get; set; }

    // Tên ca thi
    [StringLength(100)]
    public string Name { get; set; }

    // Thời gian bắt đầu và kết thúc ca thi
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    // Trạng thái hoạt động của ca thi
    // true: đang hoạt động, false: không hoạt động
    public bool IsActive { get; set; } = false;

    // Trạng thái hoàn thành của ca thi
    // true: đã thi xong, false: chưa thi xong
    public bool IsCompleted { get; set; } = false;

    // Khóa ngoại liên kết với bảng ExamBatchDetail
    // Xác định ca thi thuộc lần thi nào của đợt thi
    [ForeignKey("ExamBatchDetail")]
    public int ExamBatchDetailId { get; set; }
    public ExamBatchDetail ExamBatchDetail { get; set; }

    // Collection các môn thi trong ca thi này
    // Mối quan hệ one-to-many với ExamSessionSubject
    public ICollection<ExamSessionSubject>? ExamSessionSubjects { get; set; }
} 