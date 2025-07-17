using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace backend_manage.Entities
{
    // Entity đại diện cho chương học trong môn học
    public class Chapter : BaseEntity
    {
        // Khóa chính của bảng Chapter (tự động tăng)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ChapterId { get; set; }

        // Khóa ngoại liên kết với bảng Subject
        // Xác định chương thuộc môn học nào
        [ForeignKey("Subject")]
        public int SubjectId { get; set; }
        // Navigation property đến entity Subject
        public Subject Subject { get; set; }
        
        // Khóa ngoại liên kết với chương cha (self-referencing)
        // null nếu là chương gốc, có giá trị nếu là chương con
        public int? ParentChapterId { get; set; }

        // Tên chương học
        [StringLength(200)]
        public string Name { get; set; }

        // Mô tả chi tiết về chương học
        [StringLength(1000)]
        public string? Description { get; set; }

        // Số lượng câu hỏi trong chương này
        public int QuestionCount { get; set; }

        // Thứ tự hiển thị của chương trong môn học
        public int Order { get; set; }

        // Cờ đánh dấu chương có câu hỏi nhóm hay không
        // true: có câu hỏi nhóm, false: không có
        public bool IsGroupQuestion { get; set; }
        
        // Navigation property đến chương cha
        public Chapter? ParentChapter { get; set; }
        
        // Collection các chương con của chương này
        public ICollection<Chapter> ChildChapters { get; set; } = new List<Chapter>();

        // Collection các câu hỏi thuộc chương này
        // Mối quan hệ one-to-many với Question
        public ICollection<Question> Questions { get; set; } = new List<Question>();

        // Collection các chi tiết đề thi hoán vị có chương này
        // Mối quan hệ one-to-many với ShuffledExamPaperDetail
        public ICollection<ShuffledExamPaperDetail> ShuffledExamPaperDetails { get; set; } = new List<ShuffledExamPaperDetail>();
        
        // Collection các chi tiết đề thi gốc có chương này
        // Mối quan hệ one-to-many với OriginalExamPaperDetail
        public ICollection<OriginalExamPaperDetail> OriginalExamPaperDetails { get; set; } = new List<OriginalExamPaperDetail>();
    }
}