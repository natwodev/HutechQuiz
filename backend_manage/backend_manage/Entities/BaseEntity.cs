using System;
using backend_manage.Hubs;
using System.ComponentModel.DataAnnotations;

namespace backend_manage.Entities;

// Lớp cơ sở cho tất cả các entity trong hệ thống
// Chứa các trường audit và tracking cơ bản
public abstract class BaseEntity
{
    // Thời gian tạo bản ghi (tự động set theo giờ Việt Nam)
    public DateTime CreatedAt { get; set; } 
    
    // Người tạo bản ghi (username hoặc user ID)
    [MaxLength(50)]
    public string CreatedBy { get; set; }
    
    // Thời gian cập nhật bản ghi lần cuối (có thể null nếu chưa cập nhật)
    public DateTime? UpdatedAt { get; set; } 
    
    // Người cập nhật bản ghi lần cuối (có thể null nếu chưa cập nhật)
    [MaxLength(50)]
    public string? UpdatedBy { get; set; }
    
    // Cờ đánh dấu bản ghi đã bị xóa mềm (soft delete)
    // true: đã xóa, false: còn tồn tại
    public bool IsDeleted { get; set; } = false;
    
    // Phiên bản của bản ghi, tăng lên mỗi khi có cập nhật
    // Dùng để tracking thay đổi và conflict resolution
    public int Version { get; set; } = 1;
} 