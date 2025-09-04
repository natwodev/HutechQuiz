using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.core.Entities;

// Entity đại diện cho đáp án trong hệ thống
// Kế thừa từ BaseEntity để có các trường audit
public class Answers : BaseEntity
{
    // Khóa chính của bảng Answers (tự động tăng)
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AnswerId { get; set; }
    
    // Thứ tự của đáp án trong câu hỏi
    public int Order { get; set; }
    
    // Nội dung đáp án
    public string AnswerContent { get; set; }
    
    // Trạng thái đáp án (true: đúng, false: sai)
    public bool IsCorrect { get; set; }
    
    // Cho phép hoán vị đáp án này hay không
    public bool CanShuffleAnswer { get; set; } = true;
    
    // Khóa ngoại liên kết với OriginalExamPaperDetail (câu hỏi)
    [ForeignKey("OriginalExamPaperDetail")]
    public int OriginalExamPaperDetailId { get; set; }
    
    // Navigation property đến entity OriginalExamPaperDetail
    public OriginalExamPaperDetail OriginalExamPaperDetail { get; set; }
}