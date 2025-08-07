namespace backend_manage.shared.DTOs
{
    public class ChapterDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; }
        public int SubjectId { get; set; }
        public int? ParentChapterId { get; set; }
    }

    public class ChapterCreateDto
    {
        public string ChapterName { get; set; }
        public int SubjectId { get; set; }
        public int? ParentChapterId { get; set; }
    }

    public class ChapterUpdateDto
    {
        public string ChapterName { get; set; }
        public int SubjectId { get; set; }
        public int? ParentChapterId { get; set; }
    }
} 