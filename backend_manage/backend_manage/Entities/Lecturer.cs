using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace backend_manage.Entities
{
    // Entity đại diện cho giảng viên trong hệ thống
    public class Lecturer : BaseEntity
    {
        // Khóa chính của bảng Lecturer (tự động tăng)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LecturerId { get; set; }

        // Mã giảng viên (LecturerCode)
        [MaxLength(20)]
        public string LecturerCode { get; set; }

        // Tên của giảng viên
        [StringLength(50)]
        public string FirstName { get; set; }

        // Họ của giảng viên
        [StringLength(50)]
        public string LastName { get; set; }

        // Giới tính (true: nam, false: nữ, null: chưa xác định)
        public bool? Gender { get; set; }

        // Ngày sinh
        public DateTime? DateOfBirth { get; set; }

        // Email
        [MaxLength(100)]
        public string? Email { get; set; }

        // Số điện thoại
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        // Mã khoa (DepartmentId)
        [MaxLength(10)]
        public string DepartmentId { get; set; }

        // Navigation property đến Department
        [ForeignKey("DepartmentId")]
        public Department Department { get; set; }
        
        [MaxLength(450)]
        [ForeignKey("ApplicationUser")]
        public string? UserId { get; set; }
        [JsonIgnore]
        public ApplicationUser? ApplicationUser { get; set; }

        public ICollection<ExamRoomLecturerAssignment> ExamRoomLecturerAssignments { get; set; }
    }
} 