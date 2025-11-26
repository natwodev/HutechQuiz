using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace frontend_manage.DTOs;

public class SaveAnswerDto
{
    [Required]
    public int StudentExamSessionId { get; set; }

    [Required]
    public int key { get; set; } // key này là OriginalExamPaperDetailId

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
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
