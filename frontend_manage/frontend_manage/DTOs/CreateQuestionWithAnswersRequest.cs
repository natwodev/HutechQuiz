using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs
{
    public class CreateQuestionWithAnswersRequest
    {
        [Required(ErrorMessage = "OriginalExamPaperId là bắt buộc")]
        public int OriginalExamPaperId { get; set; }
        
        [Required(ErrorMessage = "Order là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Order phải lớn hơn 0")]
        public int Order { get; set; }
        
        public string? QuestionContent { get; set; }
        
        [Range(1, int.MaxValue, ErrorMessage = "CorrectAnswerIndex phải lớn hơn 0")]
        public int? CorrectAnswerIndex { get; set; }
        
        public int? ParentQuestionId { get; set; }
        
        public int? ChapterId { get; set; }
        
        public bool CanShuffleQuestion { get; set; } = true;
        
        public List<CreateAnswerRequest> Answers { get; set; } = new();
    }
    
    public class CreateAnswerRequest
    {
        [Required(ErrorMessage = "Order là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Order phải lớn hơn 0")]
        public int Order { get; set; }
        
        [Required(ErrorMessage = "AnswerContent là bắt buộc")]
        public string AnswerContent { get; set; } = string.Empty;
        
        public bool IsCorrect { get; set; }
        
        public bool CanShuffleAnswer { get; set; } = true;
    }
}

