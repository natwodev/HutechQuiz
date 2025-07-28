using System;
using System.ComponentModel.DataAnnotations;

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

    public class SubmitExamDto
    {
        public int ShuffledExamPaperId { get; set; }
        public List<SaveAnswerDto> SaveAnswerDtos { get; set; }
    }


    public class SaveAnswerDto
    {
        [Required]
        public int ShuffledExamPaperId { get; set; }

        [Required]
        public int Index { get; set; }

        [Required]
        public string Answer { get; set; }
    }
} 