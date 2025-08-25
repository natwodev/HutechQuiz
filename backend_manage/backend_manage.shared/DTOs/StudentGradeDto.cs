namespace backend_manage.shared.DTOs
{
    public class StudentGradeDto
    {
        public int STT { get; set; }
        public string StudentCode { get; set; }
        public double Score { get; set; }
    }

    public class ExportGradeRequestDto
    {
        public int? ExamSessionId { get; set; }  // Lọc theo ca thi
        public int? SubjectId { get; set; }      // Lọc theo môn học
        public int? ExamSessionSubjectId { get; set; } // Lọc theo môn thi cụ thể
        public DateTime? FromDate { get; set; }  // Từ ngày
        public DateTime? ToDate { get; set; }    // Đến ngày
    }
}
