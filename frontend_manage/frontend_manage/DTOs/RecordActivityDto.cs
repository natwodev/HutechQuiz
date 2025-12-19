namespace frontend_manage.DTOs;

public class RecordActivityDto
{
    public int StudentExamSessionId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty; // ScreenBlur, Screenshot, TabSwitch, Copy, Paste, etc.
    public string? Description { get; set; }
    public string? Metadata { get; set; } // JSON format
}

