namespace backend_manage.Messages
{
    public class StudentAnswerSavedMessage
    {
        public string StudentCode { get; set; }
        public int ShuffledExamPaperId { get; set; }
        public int Index { get; set; }
        public string Answer { get; set; }
        public string NewAnswersString { get; set; }
    }
}
