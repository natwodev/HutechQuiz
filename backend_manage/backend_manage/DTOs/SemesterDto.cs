using System;

namespace backend_manage.DTOs
{
    public class SemesterDto
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; }
        public int AcademicYearId { get; set; }
        public string AcademicYearName { get; set; }
    }

    public class SemesterCreateDto
    {
        public string SemesterName { get; set; }
        public int AcademicYearId { get; set; }
    }

    public class SemesterUpdateDto
    {
        public string SemesterName { get; set; }
        public int AcademicYearId { get; set; }
    }
} 