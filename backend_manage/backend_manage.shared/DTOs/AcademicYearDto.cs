namespace backend_manage.shared.DTOs
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
    }

    public class AcademicYearUpdateDto
    {
        public string YearName { get; set; }
    }
} 