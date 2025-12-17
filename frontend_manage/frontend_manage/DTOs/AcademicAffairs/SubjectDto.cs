using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class SubjectDto
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public string? DepartmentId { get; set; }
    }

    public class SubjectCreateDto
    {
        [Required(ErrorMessage = "Mã môn học không được để trống")]
        public string SubjectCode { get; set; }
        
        [Required(ErrorMessage = "Tên môn học không được để trống")]
        public string SubjectName { get; set; }
        
        public string? DepartmentId { get; set; }
    }

    public class SubjectUpdateDto
    {
        [Required(ErrorMessage = "Mã môn học không được để trống")]
        public string SubjectCode { get; set; }
        
        [Required(ErrorMessage = "Tên môn học không được để trống")]
        public string SubjectName { get; set; }
        
        public string? DepartmentId { get; set; }
    }
}

