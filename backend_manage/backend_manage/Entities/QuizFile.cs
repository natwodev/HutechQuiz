namespace backend_manage.Entities;
using backend_manage.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class QuizFile : BaseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int QuizFileId { get; set; } // Primary key

    // Foreign key to Question (nullable)
    [ForeignKey("Question")]
    public int? QuestionId { get; set; }
    public Question? Question { get; set; }

    // Foreign key to Answer (nullable)
    [ForeignKey("Answer")]
    public int? AnswerId { get; set; }
    public Answer? Answer { get; set; }

    [StringLength(255)]
    public string FileName { get; set; } // File name

    public FileType FileType { get; set; } // File type (enum)
}