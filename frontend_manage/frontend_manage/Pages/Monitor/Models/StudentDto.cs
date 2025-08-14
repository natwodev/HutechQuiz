using System;

namespace frontend_manage.Pages.Monitor.Models
{
    public class StudentDto
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public string StudentCode { get; set; }
        public string FullName { get; set; }
        public ExamStatus ExamStatus { get; set; }
        public LoginStatus LoginStatus { get; set; }
        public int ExtraTime { get; set; }
        public double? Score { get; set; }
    }

    public enum ExamStatus
    {
        NotStarted,
        TakingExam,
        Submitted
    }

    public enum LoginStatus
    {
        NotLoggedIn,
        LoggedIn
    }
}
