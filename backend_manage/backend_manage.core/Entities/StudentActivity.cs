using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.core.Entities;

/// <summary>
/// Entity đại diện cho các hành động của sinh viên trong quá trình thi
/// </summary>
public class StudentActivity : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int StudentActivityId { get; set; }

    /// <summary>
    /// Khóa ngoại liên kết với StudentExamSession
    /// </summary>
    [ForeignKey("StudentExamSession")]
    public int StudentExamSessionId { get; set; }

    /// <summary>
    /// Mã sinh viên (để query nhanh)
    /// </summary>
    [StringLength(20)]
    public string StudentCode { get; set; } = string.Empty;

    /// <summary>
    /// Loại hành động (ScreenBlur, Screenshot, TabSwitch, Copy, Paste, etc.)
    /// </summary>
    [StringLength(50)]
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết hành động
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Dữ liệu bổ sung (JSON format nếu cần)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Thời gian xảy ra hành động
    /// </summary>
    public DateTime ActivityTime { get; set; }

    /// <summary>
    /// Navigation property đến StudentExamSession
    /// </summary>
    public StudentExamSession StudentExamSession { get; set; } = null!;
}

