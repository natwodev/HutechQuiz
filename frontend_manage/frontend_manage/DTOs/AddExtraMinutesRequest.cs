using System;

namespace frontend_manage.DTOs
{
    public class AddExtraMinutesRequest
    {
        public string StudentCode { get; set; } = string.Empty;
        public int StudentExamSessionId { get; set; }
        public int ExtraMinutes { get; set; }
        public string? ReasonForExtra { get; set; }
    }
}
