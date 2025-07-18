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
        
        // Thứ tự hiển thị của câu hỏi trong đề thi
        public int Order { get; set; }

        // Nội dung câu hỏi
        public string? QuestionContent { get; set; }
        // Đáp án 1
        public string? Answer1 { get; set; }
        // Đáp án 2
        public string? Answer2 { get; set; }
        // Đáp án 3
        public string? Answer3 { get; set; }
        // Đáp án 4
        public string? Answer4 { get; set; }
        

        // Chỉ số đáp án đúng (1, 2, 3, 4)
        public int? CorrectAnswerIndex { get; set; }

        // Khóa ngoại đến câu hỏi cha (nếu có)
        public int? ParentQuestionId { get; set; }


        // Khóa ngoại liên kết với bảng Chapter
        // Xác định câu hỏi thuộc chương nào
        [ForeignKey("Chapter")]
        public int ChapterId { get; set; }
        
        // Navigation property đến câu hỏi cha
        public OriginalExamPaperDetail? ParentQuestion { get; set; }

        // Navigation property đến danh sách câu hỏi con
        public ICollection<OriginalExamPaperDetail>? ChildQuestions { get; set; }
        
        // Navigation property đến entity OriginalExamPaper
        public OriginalExamPaper OriginalExamPaper { get; set; }
        
        // Navigation property đến entity Chapter
        public Chapter Chapter { get; set; }

        // Navigation property đến danh sách các câu hỏi hoán vị
        public ICollection<ShuffledExamPaperDetail>? ShuffledExamPaperDetails { get; set; }
    }
} 