using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace frontend_manage.DTOs;

public class SaveAnswerDto
{
    [Required]
    public int StudentExamSessionId { get; set; }

    [Required]
    public int key { get; set; }

    // Dùng cho câu hỏi con trong câu cha
    public int? value { get; set; }
    
}

public class SaveAnswerResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public SaveAnswerData? Data { get; set; }
    
    // Các trường mới để xử lý lỗi
    [JsonIgnore]
    public bool IsRateLimited { get; set; }
    
    [JsonIgnore]
    public bool IsUnauthorized { get; set; }
    
    [JsonIgnore]
    public bool IsConnectionError { get; set; }
    
    [JsonIgnore]
    public int? StatusCode { get; set; }
    
    [JsonIgnore]
    public int RetryAfterSeconds { get; set; }
}

public class SaveAnswerData
{
    [JsonPropertyName("newAnswersString")]
    public string? NewAnswersString { get; set; }
}

public class SubmitExamRequest
{
    public int StudentExamSessionId { get; set; }
}

public class SubmitExamResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public SubmitExamData? Data { get; set; }
    
    // Các trường mới để xử lý lỗi
    [JsonIgnore]
    public bool IsRateLimited { get; set; }
    
    [JsonIgnore]
    public bool IsUnauthorized { get; set; }
    
    [JsonIgnore]
    public bool IsConnectionError { get; set; }
    
    [JsonIgnore]
    public int? StatusCode { get; set; }
    
    [JsonIgnore]
    public int RetryAfterSeconds { get; set; }
}

public class SubmitExamData
{
    [JsonPropertyName("studentCode")]
    public string StudentCode { get; set; } = string.Empty;
    
    [JsonPropertyName("shuffledExamPaperId")]
    public int ShuffledExamPaperId { get; set; }
    
    [JsonPropertyName("score")]
    public double? Score { get; set; }
    
    [JsonPropertyName("correctAnswers")]
    public int? CorrectAnswers { get; set; }
    
    [JsonPropertyName("totalQuestions")]
    public int? TotalQuestions { get; set; }
    
    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }
    
    [JsonPropertyName("endTime")]
    public DateTime EndTime { get; set; }
    
    [JsonPropertyName("studentAnswersString")]
    public string StudentAnswersString { get; set; } = string.Empty;
    
    [JsonPropertyName("answerKey")]
    public string AnswerKey { get; set; } = string.Empty;
}