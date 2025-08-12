using System;
using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class ExamBatchDto
    {
        public int ExamBatchId { get; set; }
        
        [Required(ErrorMessage = "Tên đợt thi không được để trống")]
        public string BatchName { get; set; }
        
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        public DateTime EndDate { get; set; }
        
        [Required(ErrorMessage = "Học kỳ không được để trống")]
        public int SemesterId { get; set; }
        
        public string SemesterName { get; set; }
        
        public bool IsActive { get; set; }
        
        public List<ExamBatchDetailDto> ExamBatchDetails { get; set; } = new List<ExamBatchDetailDto>();
    }

    public class ExamBatchCreateDto
    {
        [Required(ErrorMessage = "Tên đợt thi không được để trống")]
        public string BatchName { get; set; }
        
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        public DateTime EndDate { get; set; }
        
        [Required(ErrorMessage = "Học kỳ không được để trống")]
        public int SemesterId { get; set; }
        
        public bool IsActive { get; set; }
        
        public List<ExamBatchDetailCreateDto> ExamBatchDetails { get; set; } = new List<ExamBatchDetailCreateDto>();
    }

    public class ExamBatchUpdateDto
    {
        [Required(ErrorMessage = "Tên đợt thi không được để trống")]
        public string BatchName { get; set; }
        
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        public DateTime EndDate { get; set; }
        
        [Required(ErrorMessage = "Học kỳ không được để trống")]
        public int SemesterId { get; set; }
        
        public bool IsActive { get; set; }
        
        public List<ExamBatchDetailUpdateDto> ExamBatchDetails { get; set; } = new List<ExamBatchDetailUpdateDto>();
    }
}
