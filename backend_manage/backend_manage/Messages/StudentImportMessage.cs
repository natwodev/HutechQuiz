using System.Text.Json.Serialization;

namespace backend_manage.Messages;

public class StudentImportMessage
{
    public string JobId { get; set; } = string.Empty;
    public string FileContent { get; set; } = string.Empty; // Base64 encoded
    public string FileName { get; set; } = string.Empty;
    public string ExamSessionSubjectCore { get; set; } = string.Empty;
    public int ExamRoomId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}