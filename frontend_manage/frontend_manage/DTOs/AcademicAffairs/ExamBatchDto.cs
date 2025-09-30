namespace frontend_manage.DTOs.AcademicAffairs
{
    public class ExamBatchDto
    {
        public int ExamBatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<ExamBatchDetailDto> ExamBatchDetails { get; set; } = new();
    }

    public class ExamBatchCreateDto
    {
        public string BatchName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int SemesterId { get; set; }
        public bool IsActive { get; set; }
        public List<ExamBatchDetailCreateDto> ExamBatchDetails { get; set; } = new();
    }

    public class ExamBatchUpdateDto
    {
        public string BatchName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int SemesterId { get; set; }
        public bool IsActive { get; set; }
        public List<ExamBatchDetailUpdateDto> ExamBatchDetails { get; set; } = new();
    }

    public class ExamBatchDetailDto
    {
        public int ExamBatchDetailId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ExamBatchId { get; set; }
        public string ExamBatchName { get; set; } = string.Empty;
        public List<ExamSessionDto> ExamSessions { get; set; } = new();
    }

    public class ExamBatchDetailCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public int ExamBatchId { get; set; }
        public List<ExamSessionCreateDto> ExamSessions { get; set; } = new();
    }

    public class ExamBatchDetailUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public int ExamBatchId { get; set; }
        public List<ExamSessionUpdateDto> ExamSessions { get; set; } = new();
    }
}

