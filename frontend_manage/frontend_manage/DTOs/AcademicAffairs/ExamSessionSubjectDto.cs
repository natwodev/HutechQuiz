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
        public int ExamSessionId { get; set; }
        public int SubjectId { get; set; }
        public int Duration { get; set; }
        public int? OriginalExamPaperId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ExamSessionSubjectCore { get; set; } = string.Empty;
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
        public string ExamSessionSubjectCore { get; set; } = string.Empty;
        public int? ExamRoomId { get; set; }
        public int? MonitorId { get; set; }
    }

    public class ExamSessionSubjectWithRoomsDto
    {
        public int ExamSessionSubjectId { get; set; }
        public int ExamSessionId { get; set; }
        public string ExamSessionName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public int Duration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsCompleted { get; set; }
        public List<ExamRoomDto> ExamRooms { get; set; } = new();
    }

    public class ExamRoomDto
    {
        public int ExamRoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class AssignLecturerDto
    {
        public int ExamSessionSubjectId { get; set; }
        public int LecturerId { get; set; }
    }

    public class UnassignLecturerDto
    {
        public int ExamSessionSubjectId { get; set; }
    }
}

