using System.ComponentModel.DataAnnotations;

namespace backend_manage.shared.DTOs
{
    // DTO cho entity Answers trong database
    public class AnswerDto
    {
        public int AnswerId { get; set; }
        public int Order { get; set; }
        public string AnswerContent { get; set; }
        public bool IsCorrect { get; set; }
        public bool CanShuffleAnswer { get; set; }
        public int OriginalExamPaperDetailId { get; set; }
    }

    
    public class AnswerShufferDto
    {
        public int AnswerId { get; set; }
        public int Order { get; set; }
        public string AnswerContent { get; set; }
        public bool IsCorrect { get; set; }
        public bool CanShuffleAnswer { get; set; }
        public int OriginalExamPaperDetailId { get; set; }
    }
    
    public class AnswerCreateDto
    {
        [Required]
        public int Order { get; set; }
        
        [Required]
        public string AnswerContent { get; set; }
        
        public bool IsCorrect { get; set; }
        
        public bool CanShuffleAnswer { get; set; } = true;
        
        [Required]
        public int OriginalExamPaperDetailId { get; set; }
    }

    public class AnswerUpdateDto
    {
        public int? Order { get; set; }
        public string AnswerContent { get; set; }
        public bool? IsCorrect { get; set; }
        public bool? CanShuffleAnswer { get; set; }
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
        public int key { get; set; }
        // Dùng cho câu hỏi con trong câu cha
        public object value { get; set; }
        
    }

    public class ExamSubmissionDto
    {
        public string StudentCode { get; set; }
        public int ShuffledExamPaperId { get; set; }
        public double? Score { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalQuestions { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string StudentAnswersString { get; set; }
        public string AnswerKey { get; set; }
    }
} 