namespace backend_manage.core.Messages;

public class StartExamMessage
{
    public int StudentExamSessionId { get; set; }
    public string StudentCode { get; set; }
    public DateTime StartTime { get; set; }
    public int ShuffledExamPaperId { get; set; }
    public int OriginalExamPaperId { get; set; }
    public string StudentAnswersString { get; set; }
    public bool IsCompleted { get; set; }
    public int RemainingMinutes { get; set; }

}
