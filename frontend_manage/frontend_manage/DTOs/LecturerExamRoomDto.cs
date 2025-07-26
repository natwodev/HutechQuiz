namespace frontend_manage.DTOs
{
    public class LecturerExamRoomDto
    {
        public int ExamRoomLecturerAssignmentId { get; set; }
        public int ExamRoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int ExamSessionSubjectId { get; set; }
        public string LecturerName { get; set; } = string.Empty;
        public string LecturerCode { get; set; } = string.Empty;
        public DateTime? ExamStartTime { get; set; }
        public DateTime? ExamEndTime { get; set; }
        public string ExamStatus { get; set; } = string.Empty; // "pending", "ongoing", "completed"
    }
} 