using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class DepartmentDto
    {
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }

    public class DepartmentCreateDto
    {
        [Required(ErrorMessage = "Mã khoa không được để trống")]
        public string DepartmentId { get; set; }
        
        [Required(ErrorMessage = "Tên khoa không được để trống")]
        public string DepartmentName { get; set; }
    }

    public class DepartmentUpdateDto
    {
        [Required(ErrorMessage = "Tên khoa không được để trống")]
        public string DepartmentName { get; set; }
    }
}
