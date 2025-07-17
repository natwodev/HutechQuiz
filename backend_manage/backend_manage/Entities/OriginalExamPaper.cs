using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.Entities;

// Entity đại diện cho đề thi gốc trong hệ thống
// Kế thừa từ BaseEntity để có các trường audit
public class OriginalExamPaper : BaseEntity
{
    // Khóa chính của bảng OriginalExamPaper (tự động tăng)
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int OriginalExamPaperId { get; set; }
    
    // Mã định danh đề thi gốc (Dạng "EXP001") 
    [MaxLength(50)]
    public string OriginalExamPaperCore { get; set; }
    
    // Tiêu đề của đề thi gốc
    [StringLength(100)]
    public string Title { get; set; }
    
    // Mô tả chi tiết về đề thi gốc
    [StringLength(500)]
    public string? Description { get; set; }
    
    // Khóa ngoại liên kết với bảng Subject
    // Xác định đề thi gốc thuộc môn học nào
    [ForeignKey("Subject")]
    public int SubjectId { get; set; }
    
    // Trạng thái phê duyệt đề thi gốc
    // true: đã phê duyệt, false: chưa phê duyệt, null: chưa xét duyệt
    public bool? IsApproved { get; set; }
    
    // Thời gian làm bài (tính bằng phút)
    public int DurationMinutes { get; set; }
    
    // Tổng số câu hỏi trong đề thi gốc
    public int TotalQuestions { get; set; }
    
    // Tổng số đề hoán vị được tạo từ đề thi gốc này
    public int TotalShuffledPapers { get; set; }
    
    // Navigation property đến entity Subject
    public Subject Subject { get; set; }

    // Collection các đề thi hoán vị được tạo từ đề thi gốc này
    // Mối quan hệ one-to-many với ShuffledExamPaper
    public ICollection<ShuffledExamPaper> ShuffledExamPapers { get; set; } = new List<ShuffledExamPaper>();
    
    // Collection các ca thi sử dụng đề thi gốc này
    // Mối quan hệ one-to-many với ExamSessionSubject
    public ICollection<ExamSessionSubject> ExamSessionSubjects { get; set; } = new List<ExamSessionSubject>();
    
    // Collection các chi tiết câu hỏi trong đề thi gốc
    // Mối quan hệ one-to-many với OriginalExamPaperDetail
    public ICollection<OriginalExamPaperDetail> OriginalExamPaperDetails { get; set; } = new List<OriginalExamPaperDetail>();
    
    // Collection các chi tiết câu hỏi trong đề thi hoán vị
    // Mối quan hệ one-to-many với ShuffledExamPaperDetail
    public ICollection<ShuffledExamPaperDetail>? ShuffledExamPaperDetails { get; set; }
} 