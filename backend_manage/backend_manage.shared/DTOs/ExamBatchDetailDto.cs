namespace backend_manage.shared.DTOs
{
    public class ExamBatchDetailDto
    {
        public int ExamBatchDetailId { get; set; }
        public string Name { get; set; }

        public int ExamBatchId { get; set; }
        public string ExamBatchName { get; set; }
        public List<ExamSessionDto> ExamSessions { get; set; }

    }

    public class ExamBatchDetailCreateDto
    {
        public int ExamBatchId { get; set; }
        public string Name { get; set; }
        public List<ExamSessionCreateDto> ExamSessions { get; set; }
    }

    public class ExamBatchDetailUpdateDto
    {
        public int ExamBatchId { get; set; }
        public string Name { get; set; }
        public List<ExamSessionUpdateDto> ExamSessions { get; set; }
    }
}