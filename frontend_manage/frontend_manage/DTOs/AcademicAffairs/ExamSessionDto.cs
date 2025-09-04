using System;
using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class ExamSessionDto
    {
        public int ExamSessionId { get; set; }
        
        [Required(ErrorMessage = "Tên ca thi không được để trống")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Thời gian bắt đầu không được để trống")]
        public DateTime StartTime { get; set; }
        
        [Required(ErrorMessage = "Thời gian kết thúc không được để trống")]
        public DateTime EndTime { get; set; }
        
        public bool IsActive { get; set; }
        
        public bool IsCompleted { get; set; }
        
        [Required(ErrorMessage = "Chi tiết đợt thi không được để trống")]
        public int ExamBatchDetailId { get; set; }
        
        public string ExamBatchDetailName { get; set; }
        
        public int ExamBatchId { get; set; }
    }

    public class ExamSessionCreateDto
    {
        [Required(ErrorMessage = "Tên ca thi không được để trống")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Thời gian bắt đầu không được để trống")]
        public DateTime StartTime { get; set; }
        
        [Required(ErrorMessage = "Thời gian kết thúc không được để trống")]
        public DateTime EndTime { get; set; }
        
        public bool IsActive { get; set; }
        
        public bool IsCompleted { get; set; }
        
        [Required(ErrorMessage = "Chi tiết đợt thi không được để trống")]
        public int ExamBatchDetailId { get; set; }
    }

    public class ExamSessionUpdateDto
    {
        [Required(ErrorMessage = "Tên ca thi không được để trống")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Thời gian bắt đầu không được để trống")]
        public DateTime StartTime { get; set; }
        
        [Required(ErrorMessage = "Thời gian kết thúc không được để trống")]
        public DateTime EndTime { get; set; }
        
        public bool IsActive { get; set; }
        
        public bool IsCompleted { get; set; }
        
        [Required(ErrorMessage = "Chi tiết đợt thi không được để trống")]
        public int ExamBatchDetailId { get; set; }
    }
}
