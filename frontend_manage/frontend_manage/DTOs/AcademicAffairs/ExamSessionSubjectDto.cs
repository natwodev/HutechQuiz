using System;
using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class ExamSessionSubjectDto
    {
        public int ExamSessionSubjectId { get; set; }
        
        // Backend returns ExamSessionId, not ExamSessionDepartmentId
        public int ExamSessionId { get; set; }
        
        // Keep ExamSessionDepartmentId for backward compatibility (deprecated)
        public int ExamSessionDepartmentId { get; set; }
        
        [Required(ErrorMessage = "Môn học không được để trống")]
        public int SubjectId { get; set; }
        
        public string SubjectName { get; set; }
        
        [Required(ErrorMessage = "Thời lượng thi không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "Thời lượng thi phải lớn hơn 0")]
        public int Duration { get; set; }
        
        public int? OriginalExamPaperId { get; set; }
        
        public string OriginalExamPaperTitle { get; set; }
        
        public bool IsCompleted { get; set; }
        
        public DateTime StartTime { get; set; }
        
        public DateTime? EndTime { get; set; }
        
        public string ExamSessionSubjectCore { get; set; }
        
        public int? ExamRoomId { get; set; }
        public string RoomName { get; set; }
        
        public int? MonitorId { get; set; }
        public string MonitorName { get; set; }
    }

    public class ExamSessionSubjectCreateDto
    {
        [Required(ErrorMessage = "Khoa - Ca thi không được để trống")]
        public int ExamSessionDepartmentId { get; set; }
        
        [Required(ErrorMessage = "Môn học không được để trống")]
        public int SubjectId { get; set; }
        
        [Required(ErrorMessage = "Thời lượng thi không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "Thời lượng thi phải lớn hơn 0")]
        public int Duration { get; set; }
        
        public int? OriginalExamPaperId { get; set; }
        
        public bool IsCompleted { get; set; }
        
        public DateTime StartTime { get; set; }
        
        public DateTime? EndTime { get; set; }
        
        public string ExamSessionSubjectCore { get; set; }
        
        public int? MonitorId { get; set; }
    }

    public class ExamSessionSubjectUpdateDto
    {
        // Backend expects ExamSessionId, not ExamSessionDepartmentId
        [Required(ErrorMessage = "Ca thi không được để trống")]
        public int ExamSessionId { get; set; }
        
        // Keep ExamSessionDepartmentId for backward compatibility (deprecated)
        public int ExamSessionDepartmentId { get; set; }
        
        [Required(ErrorMessage = "Môn học không được để trống")]
        public int SubjectId { get; set; }
        
        [Required(ErrorMessage = "Thời lượng thi không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "Thời lượng thi phải lớn hơn 0")]
        public int Duration { get; set; }
        
        public int? OriginalExamPaperId { get; set; }
        
        public bool IsCompleted { get; set; }
        
        public DateTime StartTime { get; set; }
        
        public DateTime? EndTime { get; set; }
        
        public string ExamSessionSubjectCore { get; set; }
        
        public int? ExamRoomId { get; set; }
        public int? MonitorId { get; set; }
    }

    public class ExamSessionSubjectWithRoomsDto : ExamSessionSubjectDto
    {
        public List<ExamRoomDto> ExamRooms { get; set; } = new List<ExamRoomDto>();
    }

    public class ExamRoomDto
    {
        public int ExamRoomId { get; set; }
        public string RoomName { get; set; }
        public int Capacity { get; set; }
    }
}
