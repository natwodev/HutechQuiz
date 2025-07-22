namespace frontend_manage.DTOs
{
    public class StudentExamRoomStatusDto
    {
        public string StudentCode { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsLogin { get; set; }
        public bool IsCompleted { get; set; }
        public int? ExamSessionSubjectId { get; set; }
        public string? SubjectName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class StudentListResponse
    {
        public List<StudentExamRoomStatusDto> Students { get; set; }
    }
}