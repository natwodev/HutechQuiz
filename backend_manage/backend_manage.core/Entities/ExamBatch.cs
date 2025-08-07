using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.core.Entities;

// Entity đại diện cho đợt thi trong hệ thống
// Kế thừa từ BaseEntity để có các trường audit
public class ExamBatch : BaseEntity
{
    // Khóa chính của bảng ExamBatch (tự động tăng)
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ExamBatchId { get; set; }

    // Tên đợt thi
    [StringLength(100)]
    public string Name { get; set; }

    // Mô tả chi tiết về đợt thi
    [StringLength(500)]
    public string? Description { get; set; }

    // Ngày bắt đầu đợt thi
    public DateTime StartDate { get; set; }
    
    // Ngày kết thúc đợt thi
    public DateTime EndDate { get; set; }

    // Trạng thái hoạt động của đợt thi
    // true: đang hoạt động, false: không hoạt động
    public bool IsActive { get; set; } = false;

    // Trạng thái hoàn thành của đợt thi
    // true: đã thi xong, false: chưa thi xong
    public bool IsCompleted { get; set; } = false;

    // Khóa ngoại liên kết với bảng Semester
    // Xác định đợt thi thuộc học kỳ nào
    [ForeignKey("Semester")]
    public int SemesterId { get; set; }
    
    // Navigation property đến entity Semester
    public Semester? Semester { get; set; }
    public ICollection<ExamBatchDetail> ExamBatchDetails { get; set; } = new List<ExamBatchDetail>();
} 