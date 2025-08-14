using System;
using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class SemesterDto
    {
        public int SemesterId { get; set; }
        
        [Required(ErrorMessage = "Tên học kỳ không được để trống")]
        public string SemesterName { get; set; }
        
        [Required(ErrorMessage = "Năm học không được để trống")]
        public int AcademicYearId { get; set; }
        
        public string AcademicYearName { get; set; }
    }

    public class SemesterCreateDto
    {
        [Required(ErrorMessage = "Tên học kỳ không được để trống")]
        public string SemesterName { get; set; }
        
        [Required(ErrorMessage = "Năm học không được để trống")]
        public int AcademicYearId { get; set; }
    }

    public class SemesterUpdateDto
    {
        [Required(ErrorMessage = "Tên học kỳ không được để trống")]
        public string SemesterName { get; set; }
        
        [Required(ErrorMessage = "Năm học không được để trống")]
        public int AcademicYearId { get; set; }
    }
}
