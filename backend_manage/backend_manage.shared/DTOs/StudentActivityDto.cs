namespace backend_manage.shared.DTOs;

public class StudentActivityDto
{
    public int StudentActivityId { get; set; }
    public int StudentExamSessionId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Metadata { get; set; }
    public DateTime ActivityTime { get; set; }
    public string? StudentName { get; set; }
    public int CheatingWarningCount { get; set; }
}

