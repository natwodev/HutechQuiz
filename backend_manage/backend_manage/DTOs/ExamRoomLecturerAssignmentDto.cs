namespace backend_manage.DTOs
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
} 