using System;
using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class ExamRoomLecturerAssignmentDto
    {
        public int ExamRoomLecturerAssignmentId { get; set; }
        public int ExamRoomId { get; set; }
        public string RoomName { get; set; }
        public int ExamSessionSubjectId { get; set; }
        public string SubjectName { get; set; }
        public int LecturerId { get; set; }
        public string LecturerName { get; set; }
    }

    public class ExamRoomLecturerAssignmentCreateDto
    {
        [Required(ErrorMessage = "Phòng thi không được để trống")]
        public int ExamRoomId { get; set; }
        
        [Required(ErrorMessage = "Môn thi không được để trống")]
        public int ExamSessionSubjectId { get; set; }
        
        [Required(ErrorMessage = "Giảng viên không được để trống")]
        public int LecturerId { get; set; }
    }

    public class ExamRoomLecturerAssignmentUpdateDto
    {
        [Required(ErrorMessage = "Phòng thi không được để trống")]
        public int ExamRoomId { get; set; }
        
        [Required(ErrorMessage = "Môn thi không được để trống")]
        public int ExamSessionSubjectId { get; set; }
        
        [Required(ErrorMessage = "Giảng viên không được để trống")]
        public int LecturerId { get; set; }
    }
}
