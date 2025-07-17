using System;
using System.Collections.Generic;
using backend_manage.DTOs;

namespace backend_manage.DTOs
{
    public class AcademicYearDto
    {
        public int AcademicYearId { get; set; }
        public string YearName { get; set; }
        public List<SemesterDto> Semesters { get; set; }
    }

    public class AcademicYearCreateDto
    {
        public string YearName { get; set; }
        public List<SemesterCreateDto> Semesters { get; set; }

    }

    public class AcademicYearUpdateDto
    {
        public string YearName { get; set; }
        public List<SemesterUpdateDto> Semesters { get; set; }
    }
} 