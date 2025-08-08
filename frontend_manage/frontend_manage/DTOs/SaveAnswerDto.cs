using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs;

public class SaveAnswerDto
{
    [Required]
    public int StudentExamSessionId { get; set; }

    [Required]
    public int Index { get; set; }

    // Dùng cho câu hỏi con trong câu cha
    public int? SubIndex { get; set; }

    [Required]
    [RegularExpression("^[A-D]$", ErrorMessage = "Đáp án phải là A, B, C hoặc D")]
    public string Answer { get; set; } = null!;
}

public class SaveAnswerResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public SaveAnswerData? Data { get; set; }
}

public class SaveAnswerData
{
    public string? NewAnswersString { get; set; }
}