using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.core.Entities;

// Entity đại diện cho sinh viên trong hệ thống
// Kế thừa từ BaseEntity để có các trường audit
public class Student : BaseEntity
{
    // Khóa chính của bảng Student (tự động tăng)
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int StudentId { get; set; }
    
    // Mã sinh viên (có thể khác với StudentId)
    [StringLength(20)]
    public string StudentCode { get; set; }

    // Tên của sinh viên
    [StringLength(50)]
    public string FirstName { get; set; }

    // Họ của sinh viên
    [StringLength(50)]
    public string LastName { get; set; }

    // Giới tính của sinh viên
    // true: nam, false: nữ, null: chưa xác định
    public bool? Gender { get; set; } // true: male, false: female, null: unknown

    // Ngày sinh của sinh viên
    public DateTime? DateOfBirth { get; set; }

    // Trạng thái đăng nhập hiện tại của sinh viên
    // true: đang đăng nhập, false: chưa đăng nhập
    public bool IsLogin { get; set; } = false;
    
    // Thời gian đăng nhập gần nhất
    public DateTime? LastLoggedIn { get; set; }
    // Thời gian đăng xuất gần nhất
    public DateTime? LastLoggedOut { get; set; }
    
    // Collection các ca thi mà sinh viên tham gia
    // Mối quan hệ many-to-many với ExamSession
    public ICollection<StudentExamSession>? StudentExamSessions { get; set; }
}