using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.Entities
{
    public class ExamRoomLecturerAssignment : BaseEntity
    {
        public int ExamRoomLecturerAssignmentId { get; set; }
        public int ExamRoomId { get; set; }
        public ExamRoom ExamRoom { get; set; }

        public int LecturerId { get; set; }
        public Lecturer Lecturer { get; set; }

        // Có thể thêm các trường khác nếu cần, ví dụ:
        // public string AssignmentStatus { get; set; }
        // public string Note { get; set; }
    }
} 