using System;
using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class ExamBatchDetailDto
    {
        public int ExamBatchDetailId { get; set; }
        
        [Required(ErrorMessage = "Tên chi tiết đợt thi không được để trống")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Đợt thi không được để trống")]
        public int ExamBatchId { get; set; }
        
        public string ExamBatchName { get; set; }
        
        public List<ExamSessionDto> ExamSessions { get; set; } = new List<ExamSessionDto>();
    }

    public class ExamBatchDetailCreateDto
    {
        [Required(ErrorMessage = "Tên chi tiết đợt thi không được để trống")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Đợt thi không được để trống")]
        public int ExamBatchId { get; set; }
        
        public List<ExamSessionCreateDto> ExamSessions { get; set; } = new List<ExamSessionCreateDto>();
    }

    public class ExamBatchDetailUpdateDto
    {
        [Required(ErrorMessage = "Tên chi tiết đợt thi không được để trống")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Đợt thi không được để trống")]
        public int ExamBatchId { get; set; }
        
        public List<ExamSessionUpdateDto> ExamSessions { get; set; } = new List<ExamSessionUpdateDto>();
    }
}
