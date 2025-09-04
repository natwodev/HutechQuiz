namespace backend_manage.shared.DTOs
{
    public class ExamSessionSubjectDto
    {
        public int ExamSessionSubjectId { get; set; }
        public int ExamSessionId { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }
        public int Duration { get; set; }
        public int? OriginalExamPaperId { get; set; }
        public string OriginalExamPaperTitle { get; set; }
        public bool IsCompleted { get; set; }
      //  public bool IsActive { get; set; } 
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
        public int ExamSessionId { get; set; }
        public int SubjectId { get; set; }
        public int Duration { get; set; }
        public int? OriginalExamPaperId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ExamSessionSubjectCore { get; set; }
        public int? ExamRoomId { get; set; }
        public int? MonitorId { get; set; }
    }

    public class ExamSessionSubjectUpdateDto
    {
        public int ExamSessionId { get; set; }
        public int SubjectId { get; set; }
        public int Duration { get; set; }
        public int? OriginalExamPaperId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ExamSessionSubjectCore { get; set; }
        public int? ExamRoomId { get; set; }
        public int? MonitorId { get; set; }
    }

    // DTO cho việc phân công giảng viên
    public class AssignLecturerDto
    {
        public int ExamSessionSubjectId { get; set; }
        public int LecturerId { get; set; }
    }

    // DTO cho việc hủy phân công giảng viên
    public class UnassignLecturerDto
    {
        public int ExamSessionSubjectId { get; set; }
    }
} 