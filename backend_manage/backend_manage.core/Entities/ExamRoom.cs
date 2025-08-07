using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.core.Entities
{
    // Entity đại diện cho phòng thi
    public class ExamRoom : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ExamRoomId { get; set; }

        [StringLength(50)]
        public string RoomName { get; set; } // Ví dụ: "A101", "B202"

        // Navigation: mỗi phòng có nhiều sinh viên
        public ICollection<StudentExamSession> StudentExamSessions { get; set; } = new List<StudentExamSession>();
    }
} 