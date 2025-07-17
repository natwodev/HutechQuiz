using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using backend_manage.Hubs;

namespace backend_manage.Entities;

// Entity đại diện cho câu hỏi trong hệ thống
// Kế thừa từ BaseEntity để có các trường audit
public class Question : BaseEntity
{
    // Khóa chính của bảng Question (tự động tăng)
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int QuestionId { get; set; }
    
    // Khóa ngoại liên kết với bảng Chapter
    // Xác định câu hỏi thuộc chương nào
    [ForeignKey("Chapter")]
    public int ChapterId { get; set; }
    
    // Khóa ngoại liên kết với câu hỏi cha (self-referencing)
    // null nếu là câu hỏi gốc, có giá trị nếu là câu hỏi con
    // Dùng cho câu hỏi nhóm (group questions)
    [ForeignKey("ParentQuestion")]
    public int? ParentQuestionId { get; set; }

    // Nội dung câu hỏi
    [MaxLength(100)]
    public string Content { get; set; } 

    // Mức độ khó của câu hỏi
    // 1: Dễ, 2: Trung bình, 3: Khó
    public int Level { get; set; }
    
    // Số lượng câu hỏi con của câu hỏi này
    public int SubQuestionCount { get; set; }
    
    // Tổng số lần câu hỏi được sử dụng trong các đề thi
    public int UsageCount { get; set; }
    
    // Navigation property đến entity Chapter
    public Chapter Chapter { get; set; }
    
    // Navigation property đến câu hỏi cha
    public Question? ParentQuestion { get; set; }
    
    // Collection các câu hỏi con của câu hỏi này
    public ICollection<Question> SubQuestions { get; set; } = new List<Question>();
    
    // Collection các đáp án của câu hỏi này
    // Mối quan hệ one-to-many với Answer
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    
    // Collection các chi tiết đề thi hoán vị có câu hỏi này
    // Mối quan hệ one-to-many với ShuffledExamPaperDetail
    public ICollection<ShuffledExamPaperDetail> ShuffledExamPaperDetails { get; set; } = new List<ShuffledExamPaperDetail>();
    
    // Collection các chi tiết đề thi gốc có câu hỏi này
    // Mối quan hệ one-to-many với OriginalExamPaperDetail
    public ICollection<OriginalExamPaperDetail> OriginalExamPaperDetails { get; set; } = new List<OriginalExamPaperDetail>();
}