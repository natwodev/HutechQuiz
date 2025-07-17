using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.Entities
{
    // Entity đại diện cho năm học trong hệ thống
    public class AcademicYear : BaseEntity
    {
        // Khóa chính của bảng AcademicYear (tự động tăng)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AcademicYearId { get; set; }

        // Tên năm học (ví dụ: 2023-2024, 2024-2025)
        [MaxLength(9)]
        public string AcademicYearName { get; set; }
        
        // Collection các học kỳ thuộc năm học này
        // Mối quan hệ one-to-many với Semester
        public ICollection<Semester> Semesters { get; set; } = new List<Semester>();
    }
}