namespace backend_manage.shared.DTOs
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
        public int StudentCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class ExamRoomLecturerAssignmentCreateDto
    {
        public int ExamRoomId { get; set; }
        public int ExamSessionSubjectId { get; set; }
        public int LecturerId { get; set; }
    }
} 