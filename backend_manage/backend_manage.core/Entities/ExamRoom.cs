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

        // Navigation: mỗi phòng có thể được sử dụng cho nhiều ExamSessionSubject
        public ICollection<ExamSessionSubject> ExamSessionSubjects { get; set; } = new List<ExamSessionSubject>();
    }
} 