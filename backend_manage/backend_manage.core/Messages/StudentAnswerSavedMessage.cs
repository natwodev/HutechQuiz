namespace backend_manage.core.Messages
{
    public class StudentAnswerSavedMessage
    {
        public string StudentCode { get; set; }
        public int StudentExamSessionId { get; set; }
        public int key { get; set; }
        public object value { get; set; }
        public string NewAnswersString { get; set; }
    }
}
