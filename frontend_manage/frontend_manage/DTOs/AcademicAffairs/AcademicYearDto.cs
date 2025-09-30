namespace frontend_manage.DTOs.AcademicAffairs
{
    public class AcademicYearDto
    {
        public int AcademicYearId { get; set; }
        public string YearName { get; set; } = string.Empty;
        public List<SemesterDto> Semesters { get; set; } = new();
    }

    public class AcademicYearCreateDto
    {
        public string YearName { get; set; } = string.Empty;
    }

    public class AcademicYearUpdateDto
    {
        public string YearName { get; set; } = string.Empty;
    }

    public class SemesterDto
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int AcademicYearId { get; set; }
        public string AcademicYearName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class SemesterCreateDto
    {
        public string SemesterName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int AcademicYearId { get; set; }
        public bool IsActive { get; set; }
    }

    public class SemesterUpdateDto
    {
        public string SemesterName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int AcademicYearId { get; set; }
        public bool IsActive { get; set; }
    }
}
