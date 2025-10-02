namespace frontend_manage.DTOs.ExamManager
{
    public class ImportOriginalExamResponse
    {
        public string Message { get; set; }
    }

    public class CreateShuffledRequest
    {
        public string OriginalExamPaperCore { get; set; }
        public int Count { get; set; }
    }

    public class CreateShuffledResponse
    {
        public string Message { get; set; }
    }
}


