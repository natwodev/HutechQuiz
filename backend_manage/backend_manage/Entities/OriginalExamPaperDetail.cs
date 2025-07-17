using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.Entities
{
    // Entity đại diện cho chi tiết câu hỏi trong đề thi gốc
    public class OriginalExamPaperDetail : BaseEntity //câu hỏi của đề thi gốc
    {
        // Khóa chính của bảng OriginalExamPaperDetail (tự động tăng)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OriginalExamPaperDetailId { get; set; }

        // Khóa ngoại liên kết với bảng OriginalExamPaper
        // Xác định chi tiết thuộc đề thi gốc nào
        [ForeignKey("OriginalExamPaper")]
        public int OriginalExamPaperId { get; set; }

        // Khóa ngoại liên kết với bảng Chapter
        // Xác định câu hỏi thuộc chương nào
        [ForeignKey("Chapter")]
        public int ChapterId { get; set; }

        // Khóa ngoại liên kết với bảng Question
        // Xác định câu hỏi cụ thể
        [ForeignKey("Question")]
        public int QuestionId { get; set; }
        
        // Thứ tự hiển thị của câu hỏi trong đề thi
        public int Order { get; set; }
        
        // Navigation property đến entity OriginalExamPaper
        public OriginalExamPaper OriginalExamPaper { get; set; }
        
        // Navigation property đến entity Chapter
        public Chapter Chapter { get; set; }
        
        // Navigation property đến entity Question
        public Question Question { get; set; }
    }
} 