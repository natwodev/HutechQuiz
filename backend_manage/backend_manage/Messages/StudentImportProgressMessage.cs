using backend_manage.DTOs;

namespace backend_manage.Messages;

public class StudentImportProgressMessage
{
    public string JobId { get; set; } = string.Empty;
    public int Progress { get; set; } // 0-100
    public string Status { get; set; } = string.Empty; // "Processing", "Completed", "Failed"
    public string Message { get; set; } = string.Empty;
    public StudentImportResultDto? Result { get; set; }
    public DateTime UpdatedAt { get; set; }
}