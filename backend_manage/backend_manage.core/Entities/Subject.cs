using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.core.Entities
{
    // Entity đại diện cho môn học trong hệ thống
    // Kế thừa từ BaseEntity để có các trường audit
    public class Subject : BaseEntity
    {
        // Khóa chính của bảng Subject (tự động tăng)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SubjectId { get; set; }

        // Mã môn học (SubjectCore)
        [MaxLength(20)]
        public string SubjectCore { get; set; }

        // Tên môn học
        [MaxLength(100)]
        public string SubjectName { get; set; }

        // Foreign key đến bảng Department (mã khoa)
        [MaxLength(10)]
        public string? DepartmentId { get; set; }

        // Navigation property đến Department
        // Mối quan hệ many-to-one với Department
        [ForeignKey("DepartmentId")]
        public Department Department { get; set; }//

        // Collection các chương thuộc môn học này
        // Mối quan hệ one-to-many với Chapter
        public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
        
        // Collection các ca thi có môn học này
        // Mối quan hệ many-to-many với ExamSession
        public ICollection<ExamSessionSubject> ExamSessionSubjects { get; set; } = new List<ExamSessionSubject>();
        
        public ICollection<ShuffledExamPaper> ShuffledExamPapers { get; set; } = new List<ShuffledExamPaper>();
        
        // Collection các đề thi gốc thuộc môn học này
        // Mối quan hệ one-to-many với OriginalExamPaper
        public ICollection<OriginalExamPaper> OriginalExamPapers { get; set; } = new List<OriginalExamPaper>();
    }
} 