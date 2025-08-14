using System;
using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class ExamSessionDepartmentDto
    {
        public int ExamSessionDepartmentId { get; set; }
        
        [Required(ErrorMessage = "Ca thi không được để trống")]
        public int ExamSessionId { get; set; }
        
        public string ExamSessionName { get; set; }
        
        [Required(ErrorMessage = "Khoa không được để trống")]
        public string DepartmentId { get; set; }
        
        public string DepartmentName { get; set; }
    }

    public class ExamSessionDepartmentCreateDto
    {
        [Required(ErrorMessage = "Ca thi không được để trống")]
        public int ExamSessionId { get; set; }
        
        [Required(ErrorMessage = "Khoa không được để trống")]
        public string DepartmentId { get; set; }
    }

    public class ExamSessionDepartmentUpdateDto
    {
        [Required(ErrorMessage = "Ca thi không được để trống")]
        public int ExamSessionId { get; set; }
        
        [Required(ErrorMessage = "Khoa không được để trống")]
        public string DepartmentId { get; set; }
    }
}
