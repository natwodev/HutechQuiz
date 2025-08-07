using backend_manage.shared.DTOs;

namespace backend_manage.core.Messages;

public class StudentImportProgressMessage
{
    public string JobId { get; set; } = string.Empty;
    public int Progress { get; set; } // 0-100
    public string Status { get; set; } = string.Empty; // "Processing", "Completed", "Failed"
    public string Message { get; set; } = string.Empty;
    public StudentImportResultDto? Result { get; set; }
    public DateTime UpdatedAt { get; set; }
}