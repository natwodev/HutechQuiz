using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class ExamSessionSubjectDto
    {
        public int ExamSessionSubjectId { get; set; }
        public int ExamSessionId { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int? OriginalExamPaperId { get; set; }
        public string OriginalExamPaperTitle { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ExamSessionSubjectCore { get; set; } = string.Empty;
        public int? ExamRoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int? MonitorId { get; set; }
        public string MonitorName { get; set; } = string.Empty;
    }

    public class ExamSessionSubjectCreateDto
    {
        [Required(ErrorMessage = "Ca thi là bắt buộc")]
        public int ExamSessionId { get; set; }

        [Required(ErrorMessage = "Môn học là bắt buộc")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Thời lượng là bắt buộc")]
        [Range(1, 300, ErrorMessage = "Thời lượng phải từ 1 đến 300 phút")]
        public int Duration { get; set; }

        public int? OriginalExamPaperId { get; set; }
        public bool IsCompleted { get; set; } = false;

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [Required(ErrorMessage = "Mã ca thi môn học là bắt buộc")]
        [StringLength(50, ErrorMessage = "Mã ca thi môn học không được vượt quá 50 ký tự")]
        public string ExamSessionSubjectCore { get; set; } = string.Empty;

        public int? ExamRoomId { get; set; }
        public int? MonitorId { get; set; }
    }

    public class ExamSessionSubjectUpdateDto
    {
        [Required(ErrorMessage = "Ca thi là bắt buộc")]
        public int ExamSessionId { get; set; }

        [Required(ErrorMessage = "Môn học là bắt buộc")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Thời lượng là bắt buộc")]
        [Range(1, 300, ErrorMessage = "Thời lượng phải từ 1 đến 300 phút")]
        public int Duration { get; set; }

        public int? OriginalExamPaperId { get; set; }
        public bool IsCompleted { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [Required(ErrorMessage = "Mã ca thi môn học là bắt buộc")]
        [StringLength(50, ErrorMessage = "Mã ca thi môn học không được vượt quá 50 ký tự")]
        public string ExamSessionSubjectCore { get; set; } = string.Empty;

        public int? ExamRoomId { get; set; }
        public int? MonitorId { get; set; }
    }

    public class ExamSessionSubjectWithRoomsDto : ExamSessionSubjectDto
    {
        public List<ExamSessionSubjectRoomDto> Rooms { get; set; } = new();
    }

    public class ExamSessionSubjectRoomDto
    {
        public int ExamRoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public bool IsAvailable { get; set; }
    }
}



