using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.core.Entities
{
    // Entity đại diện cho môn thi trong một ca thi cụ thể
    // Liên kết ExamSessionDepartment với Subject
    public class  ExamSessionSubject : BaseEntity
    {
        // Khóa chính của bảng ExamSessionSubject (tự động tăng)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ExamSessionSubjectId { get; set; }

        // Khóa ngoại liên kết với bảng ExamSessionDepartment
        // Xác định khoa và ca thi
        [ForeignKey("ExamSessionDepartment")]
        public int ExamSessionDepartmentId { get; set; }

        // Khóa ngoại liên kết với bảng Subject
        // Xác định môn học được thi
        [ForeignKey("Subject")]
        public int SubjectId { get; set; }

        
        [MaxLength(50)]
        public string ExamSessionSubjectCore { get; set; }
        
        // Thời lượng làm bài thi (tính bằng phút)
        // Mặc định 120 phút (2 giờ) nếu không được cấu hình
        public int Duration { get; set; }
        
        
        // Khóa ngoại liên kết với bảng OriginalExamPaper
        // Xác định đề thi gốc được sử dụng cho môn thi này
        [ForeignKey("OriginalExamPaper")]
        public int? OriginalExamPaperId { get; set; }
        
        // Trạng thái hoàn thành của ca thi
        // true: đã thi xong, false: chưa thi xong
        public bool IsCompleted { get; set; } = false;

        // Thời gian được phép bắt đầu làm bài và thời gian kết thúc làm bài cho môn thi này trong ca thi
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        
        // Navigation property đến entity ExamSessionDepartment
        public ExamSessionDepartment ExamSessionDepartment { get; set; }

        
        // Navigation property đến entity Subject
        public Subject Subject { get; set; }
        
        // Navigation property đến entity OriginalExamPaper
        public OriginalExamPaper? OriginalExamPaper { get; set; }

        // Collection các đề thi hoán vị của môn học này trong ca thi
        // Mối quan hệ one-to-many với ShuffledExamPaper
        public ICollection<ShuffledExamPaper> ShuffledExamPapers { get; set; } = new List<ShuffledExamPaper>();
        
        // Collection các sinh viên tham gia thi môn này trong ca thi
        // Mối quan hệ one-to-many với StudentExamSession
        public ICollection<StudentExamSession> StudentExamSessions { get; set; } = new List<StudentExamSession>();


    }
} 