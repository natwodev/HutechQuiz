namespace frontend_manage.DTOs.AcademicAffairs
{
    public class ExamSessionDto
    {
        public int ExamSessionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
        public int ExamBatchDetailId { get; set; }
        public string ExamBatchDetailName { get; set; } = string.Empty;
    }

    public class ExamSessionCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
        public int ExamBatchDetailId { get; set; }
    }

    public class ExamSessionUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
        public int ExamBatchDetailId { get; set; }
    }
}

