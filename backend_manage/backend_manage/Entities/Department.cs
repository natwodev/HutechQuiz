using System.ComponentModel.DataAnnotations;

namespace backend_manage.Entities
{
    // Entity đại diện cho khoa trong hệ thống
    public class Department : BaseEntity
    {
        // Khóa chính của bảng Department (mã khoa)
        [Key]
        [MaxLength(10)]
        public string DepartmentId { get; set; }
        
        // Tên khoa
        [MaxLength(100)]
        public string? DepartmentName { get; set; }
        
        // Collection các môn học thuộc khoa này
        // Mối quan hệ one-to-many với Subject
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
        
        // Collection các ca thi mà khoa tham gia
        // Mối quan hệ many-to-many với ExamSession
        public ICollection<ExamSessionDepartment> ExamSessionDepartments { get; set; } = new List<ExamSessionDepartment>();
    }
}