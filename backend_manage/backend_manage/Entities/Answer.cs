using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_manage.Entities;

// Entity đại diện cho đáp án của câu hỏi
// Kế thừa từ BaseEntity để có các trường audit
public class Answer : BaseEntity
{
    // Khóa chính của bảng Answer (tự động tăng)
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AnswerId { get; set; }
    
    // Khóa ngoại liên kết với bảng Question
    // Xác định đáp án thuộc câu hỏi nào
    [ForeignKey("Question")]
    public int QuestionId { get; set; }
    public Question Question { get; set; }
    // Nội dung đáp án
    [MaxLength(100)]
    public string Content { get; set; }
    
    // Thứ tự hiển thị của đáp án trong câu hỏi
    // Dùng để sắp xếp các đáp án theo thứ tự mong muốn
    public int Order { get; set; }
    
    // Cờ đánh dấu đáp án đúng hay sai
    // true: đáp án đúng, false: đáp án sai
    public bool IsCorrect { get; set; }
    
    // Cờ đánh dấu đáp án có bị hoán vị thứ tự hay không
    // true: đã hoán vị, false: chưa hoán vị
    public bool IsShuffled { get; set; }
    
    // Navigation property đến entity Question

}