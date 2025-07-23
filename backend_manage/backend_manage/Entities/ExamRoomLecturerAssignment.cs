using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.Entities
{
    public class ExamRoomLecturerAssignment : BaseEntity
    {
        // Khóa chính của bảng ExamSessionDepartment (tự động tăng)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ExamRoomLecturerAssignmentId { get; set; }
        public int ExamRoomId { get; set; }
        public ExamRoom ExamRoom { get; set; }
        
        public int ExamSessionSubjectId { get; set; }
        public ExamSessionSubject ExamSessionSubject { get; set; }
        public int LecturerId { get; set; }
        public Lecturer Lecturer { get; set; }
        
    }
} 