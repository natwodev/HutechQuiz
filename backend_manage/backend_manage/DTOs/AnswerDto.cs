using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

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
        public int StudentExamSessionId { get; set; }

        [Required]
        public int Index { get; set; }

        [Required]
        public string Answer { get; set; }
    }

    public class ExamSubmissionDto
    {
        public string StudentCode { get; set; }
        public int ShuffledExamPaperId { get; set; }
        public double? Score { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalQuestions { get; set; }
        public DateTime EndTime { get; set; }
        public string StudentAnswersString { get; set; }
    }
} 