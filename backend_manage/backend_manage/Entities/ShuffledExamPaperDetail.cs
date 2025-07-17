using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.Entities
{
    // Entity đại diện cho chi tiết câu hỏi trong đề thi hoán vị
    public class ShuffledExamPaperDetail : BaseEntity //câu hỏi của đề thi hoán vị hoán vị
    {
        // Khóa chính của bảng ExamPaperDetail (tự động tăng)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ShuffledExamPaperDetailId { get; set; }


        // Khóa ngoại liên kết với bảng ShuffledExamPaper
        // Xác định chi tiết thuộc đề thi hoán vị nào
        [ForeignKey("ShuffledExamPaper")]
        public int ShuffledExamPaperId { get; set; }
        
        // Thứ tự hiển thị của câu hỏi trong đề thi
        public int Order { get; set; }

        // Thứ tự hoán vị đáp án, ví dụ: "1234", "4213"
        public string? AnswerOrder { get; set; }

        // Khóa ngoại liên kết với câu hỏi gốc
        public int OriginalExamPaperDetailId { get; set; }
        public OriginalExamPaperDetail OriginalExamPaperDetail { get; set; }

        // Khóa ngoại đến câu hỏi cha (nếu có)
        public int? ParentQuestionId { get; set; }

        // Navigation property đến câu hỏi cha
        public ShuffledExamPaperDetail? ParentQuestion { get; set; }

        // Navigation property đến danh sách câu hỏi con
        public ICollection<ShuffledExamPaperDetail>? ChildQuestions { get; set; }
        
        // Navigation property đến entity ShuffledExamPaper
        public ShuffledExamPaper ShuffledExamPaper { get; set; }
        
    }
} 