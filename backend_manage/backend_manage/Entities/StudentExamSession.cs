using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using backend_manage.Hubs;

namespace backend_manage.Entities;

// Entity đại diện cho phiên thi của sinh viên
// Kế thừa từ TimeRangeEntity để có các trường audit
public class StudentExamSession : BaseEntity
{
    // Khóa chính của bảng StudentExamSession (tự động tăng)
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int StudentExamSessionId { get; set; }

    // Thời gian bắt đầu làm bài và kết thúc làm bài thi của sinh viên (lúc sinh viên bấm làm bài và nộp bài)
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    // Mã sinh viên (có thể khác với StudentId)
    [StringLength(20)]
    public string StudentCode { get; set; }
    
    // Khóa ngoại liên kết với bảng Student
    // Xác định sinh viên tham gia thi
    [ForeignKey("Student")]
    public int StudentId { get; set; }
    
    // Khóa ngoại liên kết với bảng ExamSessionSubject
    // Xác định môn thi và ca thi mà sinh viên tham gia
    [ForeignKey("ExamSessionSubject")]
    public int ExamSessionSubjectId { get; set; }
    
    // Khóa ngoại liên kết với bảng ShuffledExamPaper
    // Xác định đề thi hoán vị mà sinh viên làm
    [ForeignKey("ShuffledExamPaper")]
    public int? ShuffledExamPaperId { get; set; }

    // Số phút được cộng thêm cho sinh viên (nếu có)
    public int ExtraMinutes { get; set; }

    // Lý do được gia hạn thêm thời gian
    [MaxLength(200)]
    public string? ReasonForExtra { get; set; }

    // Số câu trả lời đúng của sinh viên
    public int? CorrectAnswers { get; set; }
    
    // Tổng số câu hỏi trong bài thi
    public int? TotalQuestions { get; set; }

    // Điểm số của sinh viên (tính theo phần trăm hoặc thang điểm 10)
    public double Score { get; set; } = 0;

    // Trạng thái hoàn thành bài thi
    // true: đã hoàn thành, false: chưa hoàn thành
    public bool IsCompleted { get; set; }
    
    // Khóa ngoại liên kết với bảng ExamRoom (phòng thi)
    [ForeignKey("ExamRoom")]
    public int? ExamRoomId { get; set; }
    
    // Chuỗi lưu đáp án của sinh viên, ví dụ: "A,B,C,D,..." hoặc JSON
    public string StudentAnswersString { get; set; }

    
    // Navigation property đến entity Student
    public Student Student { get; set; }
    
    // Navigation property đến entity ExamSessionSubject
    public ExamSessionSubject ExamSessionSubject { get; set; }
    
    // Navigation property đến entity ShuffledExamPaper
    public ShuffledExamPaper ShuffledExamPaper { get; set; }

    // Navigation property đến entity ExamRoom
    public ExamRoom ExamRoom { get; set; }

}



