using System;

namespace backend_manage.DTOs
{
    public class AnswerDto
    {
        public int AnswerId { get; set; }
        public int QuestionId { get; set; }
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class AnswerCreateDto
    {
        public int QuestionId { get; set; }
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class AnswerUpdateDto
    {
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
    }
} 