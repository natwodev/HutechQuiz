namespace backend_manage.shared.DTOs;

/// <summary>
/// DTO cho phép test đề hoán vị mà không cần student session
/// Giống với StartExamResponseDto nhưng không có studentSession
/// </summary>
public class TestExamPaperDto
{
    public ShuffledExamPaperDto ExamPaper { get; set; } = null!;
    public OriginalExamPaperDto OriginalExamPaper { get; set; } = null!;
    public int DurationMinutes { get; set; }
    public string FolderName { get; set; } = string.Empty;
}
