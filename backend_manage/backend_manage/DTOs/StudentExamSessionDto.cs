using System;

namespace backend_manage.DTOs
{
    public class StudentExamSessionDto
    {
        public int ExamSessionSubjectId { get; set; }
        public string SubjectName { get; set; } 
        public string RoomName { get; set; } 
        public int Duration { get; set; }
        public int ExtraMinutes { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int StudentExamSessionId { get; set; }
    }
} 