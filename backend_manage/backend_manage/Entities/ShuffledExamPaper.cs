using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using backend_manage.Hubs;

namespace backend_manage.Entities;

// Entity đại diện cho đề thi hoán vị trong hệ thống
// Kế thừa từ BaseEntity để có các trường audit
public class ShuffledExamPaper : BaseEntity  //đề thi đã được hoán vị
{
    // Khóa chính của bảng ShuffledExamPaper (tự động tăng)
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ShuffledExamPaperId { get; set; }
    
    // Tiêu đề của đề thi hoán vị
    [StringLength(100)]
    public string Title { get; set; }
    
    // Mã định danh đề thi hoán vị (ví dụ: "SHP001")
    [MaxLength(50)]
    public string ShuffledExamPaperCore { get; set; }
    
    // Khóa ngoại liên kết với bảng OriginalExamPaper
    // Xác định đề thi hoán vị thuộc đề thi gốc nào
    [ForeignKey("OriginalExamPaper")]
    public int OriginalExamPaperId { get; set; }
    
    // Khóa ngoại liên kết với bảng Subject
    // Xác định đề thi hoán vị thuộc môn học nào
    [ForeignKey("Subject")]
    public int SubjectId { get; set; }
    
    // Trạng thái phê duyệt đề thi hoán vị
    // true: đã phê duyệt, false: chưa phê duyệt, null: chưa xét duyệt
    public bool? IsApproved { get; set; }
    
    // Total number of times this shuffled exam paper has been used for testing
    public int TotalUsageCount { get; set; } = 0;
    
    // Chuỗi đáp án đúng của đề hoán vị (ví dụ: "ABCDACDB...")
    public string? AnswerKey { get; set; }
    
    // Navigation property đến entity OriginalExamPaper
    public OriginalExamPaper OriginalExamPaper { get; set; }
    
    // Navigation property đến entity Subject
    public Subject Subject { get; set; }

    // Collection các chi tiết câu hỏi trong đề thi hoán vị
    // Mối quan hệ one-to-many với ShuffledExamPaperDetail
    public ICollection<ShuffledExamPaperDetail> ShuffledExamPaperDetails { get; set; } = new List<ShuffledExamPaperDetail>();
    
    // Collection các sinh viên làm đề thi hoán vị này
    // Mối quan hệ one-to-many với StudentExamSession
    public ICollection<StudentExamSession> StudentExamSessions { get; set; } = new List<StudentExamSession>();
}
