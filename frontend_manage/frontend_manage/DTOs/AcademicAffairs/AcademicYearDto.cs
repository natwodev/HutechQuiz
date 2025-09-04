using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class AcademicYearDto
    {
        public int AcademicYearId { get; set; }
        
        [Required(ErrorMessage = "Tên năm học không được để trống")]
        public string YearName { get; set; }
        
        public List<SemesterDto> Semesters { get; set; } = new List<SemesterDto>();
    }

    public class AcademicYearCreateDto
    {
        [Required(ErrorMessage = "Tên năm học không được để trống")]
        public string YearName { get; set; }
    }

    public class AcademicYearUpdateDto
    {
        [Required(ErrorMessage = "Tên năm học không được để trống")]
        public string YearName { get; set; }
    }
}
