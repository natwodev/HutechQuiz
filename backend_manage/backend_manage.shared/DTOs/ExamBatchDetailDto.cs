namespace backend_manage.shared.DTOs
{
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
        public int ExamBatchId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ExamSessionCreateDto> ExamSessions { get; set; } = new();
    }

    public class ExamBatchDetailUpdateDto
    {
        public int ExamBatchId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ExamSessionUpdateDto> ExamSessions { get; set; } = new();
    }
}