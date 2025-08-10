using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace frontend_manage.DTOs.AcademicAffairs
{
    public class ShuffledExamPaperDto
    {
        public int ShuffledExamPaperId { get; set; }
        public string ShuffledExamPaperCore { get; set; }
        public string Title { get; set; }
        public int OriginalExamPaperId { get; set; }
        public int SubjectId { get; set; }
        public bool IsApproved { get; set; }
        public string AnswerKey { get; set; }
        public string SubjectName { get; set; }
        public string SubjectCode { get; set; }
        public int? ExamSessionSubjectId { get; set; }
        public List<ShuffledExamPaperDetailDto> Details { get; set; } = new List<ShuffledExamPaperDetailDto>();
    }

    public class ShuffledExamPaperDetailDto
    {
        public int ShuffledExamPaperDetailId { get; set; }
        public int Order { get; set; }
        public string AnswerOrder { get; set; }
        public int OriginalExamPaperDetailId { get; set; }
        public int? ParentQuestionId { get; set; }
        public string QuestionContent { get; set; }
        public string Answer1 { get; set; }
        public string Answer2 { get; set; }
        public string Answer3 { get; set; }
        public string Answer4 { get; set; }
        public List<ShuffledExamPaperDetailDto> ChildQuestions { get; set; } = new List<ShuffledExamPaperDetailDto>();
    }

    public class ShuffledExamPaperCreateDto
    {
        [Required(ErrorMessage = "Mã đề thi hoán vị không được để trống")]
        public string ShuffledExamPaperCore { get; set; }
        
        [Required(ErrorMessage = "Tiêu đề đề thi không được để trống")]
        public string Title { get; set; }
        
        [Required(ErrorMessage = "Đề thi gốc không được để trống")]
        public int OriginalExamPaperId { get; set; }
        
        [Required(ErrorMessage = "Môn học không được để trống")]
        public int SubjectId { get; set; }
    }

    public class ShuffledExamPaperUpdateDto
    {
        [Required(ErrorMessage = "Mã đề thi hoán vị không được để trống")]
        public string ShuffledExamPaperCore { get; set; }
        
        [Required(ErrorMessage = "Tiêu đề đề thi không được để trống")]
        public string Title { get; set; }
        
        [Required(ErrorMessage = "Đề thi gốc không được để trống")]
        public int OriginalExamPaperId { get; set; }
        
        [Required(ErrorMessage = "Môn học không được để trống")]
        public int SubjectId { get; set; }
        
        public bool IsApproved { get; set; }
    }
}
