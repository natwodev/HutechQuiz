using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.Entities
{
    // Entity đại diện cho mối quan hệ many-to-many giữa ExamSession và Department
    // Xác định các khoa tham gia trong một ca thi
    public class ExamSessionDepartment : BaseEntity
    {
        // Khóa chính của bảng ExamSessionDepartment (tự động tăng)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ExamSessionDepartmentId { get; set; }

        // Khóa ngoại liên kết với bảng ExamSession
        // Xác định ca thi
        [ForeignKey("ExamSession")]
        public int ExamSessionId { get; set; }

        // Khóa ngoại liên kết với bảng Department
        // Xác định khoa tham gia
        [ForeignKey("Department")]
        [MaxLength(10)]
        public string DepartmentId { get; set; }
        
        // Navigation property đến entity ExamSession
        public ExamSession ExamSession { get; set; }
        
        // Navigation property đến entity Department
        public Department Department { get; set; }

        // Collection các môn thi của khoa trong ca thi này
        // Mối quan hệ one-to-many với ExamSessionSubject
        public ICollection<ExamSessionSubject> ExamSessionSubjects { get; set; } = new List<ExamSessionSubject>();
    }
} 