namespace backend_manage.shared.DTOs;

public class AddExtraMinutesDto
{
    public string StudentCode { get; set; } = default!;
    public int StudentExamSessionId { get; set; }
    public int ExtraMinutes { get; set; }
    public string? ReasonForExtra { get; set; }
}