using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using backend_manage.Hubs;

namespace backend_manage.Entities;

// Entity đại diện cho câu trả lời của sinh viên
// Kế thừa từ BaseEntity để có các trường audit
public class StudentAnswer : BaseEntity
{
    // Khóa chính của bảng StudentAnswer (tự động tăng)
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int StudentAnswerId { get; set; }

    // Khóa ngoại liên kết với bảng StudentExamSession
    // Xác định câu trả lời thuộc phiên thi nào của sinh viên nào
    [ForeignKey("StudentExamSession")]
    public int StudentExamSessionId { get; set; }
    
    // Khóa ngoại liên kết với bảng ExamPaperDetail
    // Xác định câu trả lời cho câu hỏi nào trong đề thi
    [ForeignKey("ExamPaperDetail")]
    public int ExamPaperDetailId { get; set; }

    // Khóa ngoại liên kết với bảng Answer
    // Xác định đáp án mà sinh viên đã chọn
    // null nếu sinh viên chưa trả lời
    [ForeignKey("SelectedAnswer")]
    public int? SelectedAnswerId { get; set; }
    
    // Navigation property đến entity StudentExamSession
    public StudentExamSession StudentExamSession { get; set; }
    
    // Navigation property đến entity ExamPaperDetail
    public ShuffledExamPaperDetail ShuffledExamPaperDetail { get; set; }
    
    // Navigation property đến entity Answer (đáp án được chọn)
    public Answer? SelectedAnswer { get; set; }
} 