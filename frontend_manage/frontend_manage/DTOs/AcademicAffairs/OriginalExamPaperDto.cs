using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class OriginalExamPaperDto
    {
        public int OriginalExamPaperId { get; set; }
        public string OriginalExamPaperCore { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int SubjectId { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public List<OriginalExamPaperDetailDto> Details { get; set; } = new List<OriginalExamPaperDetailDto>();
    }

    public class OriginalExamPaperDetailDto
    {
        public int OriginalExamPaperDetailId { get; set; }
        public int Order { get; set; }
        public string QuestionContent { get; set; }
        public string Answer1 { get; set; }
        public string Answer2 { get; set; }
        public string Answer3 { get; set; }
        public string Answer4 { get; set; }
        public int? CorrectAnswerIndex { get; set; }
        public int? ParentQuestionId { get; set; }
        public int ChapterId { get; set; }
        public bool CanShuffleQuestion { get; set; }
        public string AnswerShuffleInfo { get; set; }
        public OriginalExamPaperDetailDto ParentQuestion { get; set; }
        public List<OriginalExamPaperDetailDto> ChildQuestions { get; set; } = new List<OriginalExamPaperDetailDto>();
    }

    public class OriginalExamPaperCreateDto
    {
        [Required(ErrorMessage = "Mã đề thi không được để trống")]
        public string OriginalExamPaperCore { get; set; }
        
        [Required(ErrorMessage = "Tiêu đề đề thi không được để trống")]
        public string Title { get; set; }
        
        public string Description { get; set; }
        
        [Required(ErrorMessage = "Môn học không được để trống")]
        public int SubjectId { get; set; }
        
        [Required(ErrorMessage = "Thời gian thi không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "Thời gian thi phải lớn hơn 0")]
        public int DurationMinutes { get; set; }
    }

    public class OriginalExamPaperUpdateDto
    {
        [Required(ErrorMessage = "Mã đề thi không được để trống")]
        public string OriginalExamPaperCore { get; set; }
        
        [Required(ErrorMessage = "Tiêu đề đề thi không được để trống")]
        public string Title { get; set; }
        
        public string Description { get; set; }
        
        [Required(ErrorMessage = "Môn học không được để trống")]
        public int SubjectId { get; set; }
        
        [Required(ErrorMessage = "Thời gian thi không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "Thời gian thi phải lớn hơn 0")]
        public int DurationMinutes { get; set; }
    }
}
