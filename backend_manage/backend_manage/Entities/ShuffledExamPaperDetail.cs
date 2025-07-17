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
        
        
        // Navigation property đến entity ShuffledExamPaper
        public ShuffledExamPaper ShuffledExamPaper { get; set; }
        
        // Navigation property đến entity Chapter
        public Chapter Chapter { get; set; }
        
        // Navigation property đến entity Question
        public Question Question { get; set; }

        // Collection các câu trả lời của sinh viên cho câu hỏi này
        // Mối quan hệ one-to-many với StudentAnswer
        public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
    }
} 