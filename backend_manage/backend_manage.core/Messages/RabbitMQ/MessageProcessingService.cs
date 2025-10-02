using backend_manage.core.Data;
using backend_manage.core.Hubs;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace backend_manage.core.Messages.RabbitMQ;

public interface IMessageProcessingService
{
    Task ProcessMessageAsync<T>(string queueName, T message);
}

public class MessageProcessingService : IMessageProcessingService
{
    private readonly ILogger<MessageProcessingService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public MessageProcessingService(
        ILogger<MessageProcessingService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task ProcessMessageAsync<T>(string queueName, T message)
    {
        try
        {
            _logger.LogInformation("🔄 Xử lý message trực tiếp cho queue {QueueName}", queueName);

            // Xử lý message trực tiếp dựa trên queue name
            switch (queueName)
            {
                case "start_exam_queue":
                    await ProcessStartExamMessageAsync(message);
                    break;
                case "save_answer_queue":
                    await ProcessSaveAnswerMessageAsync(message);
                    break;
                case "exam_submission_queue":
                    await ProcessExamSubmissionMessageAsync(message);
                    break;
                // Thêm các case khác cho các loại message khác
                // case "student_import_queue":
                //     await ProcessStudentImportMessageAsync(message);
                //     break;
                default:
                    _logger.LogWarning("⚠️ Không biết cách xử lý queue {QueueName}, bỏ qua message", queueName);
                    break;
            }

            _logger.LogInformation("✅ Đã xử lý message trực tiếp cho queue {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi xử lý message trực tiếp cho queue {QueueName}", queueName);
            throw;
        }
    }

    private async Task ProcessStartExamMessageAsync<T>(T message)
    {
        try
        {
            if (message is StartExamMessage startExamMessage)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var studentExamSession = dbContext.StudentExamSessions
                    .SingleOrDefault(x => x.StudentExamSessionId == startExamMessage.StudentExamSessionId);

                if (studentExamSession == null)
                {
                    _logger.LogWarning("❗ Không tìm thấy StudentExamSession. StudentExamSessionId: {StudentExamSessionId}",
                        startExamMessage.StudentExamSessionId);
                    return;
                }

                // Cập nhật thông tin bắt đầu làm bài
                studentExamSession.StartTime = startExamMessage.StartTime;
                studentExamSession.ShuffledExamPaperId = startExamMessage.ShuffledExamPaperId;
                studentExamSession.StudentAnswersString = startExamMessage.StudentAnswersString;
                studentExamSession.IsCompleted = startExamMessage.IsCompleted;
                studentExamSession.RemainingMinutes = startExamMessage.RemainingMinutes;

                await dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "📝 Đã cập nhật bắt đầu làm bài trực tiếp: StudentCode={StudentCode}, ExamSessionId={StudentExamSessionId}, PaperId={ShuffledExamPaperId}, StartTime={StartTime}",
                    startExamMessage.StudentCode, startExamMessage.StudentExamSessionId, startExamMessage.ShuffledExamPaperId, startExamMessage.StartTime
                );
            }
            else
            {
                _logger.LogWarning("⚠️ Message không phải là StartExamMessage: {MessageType}", typeof(T).Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi xử lý StartExamMessage trực tiếp: {Message}", JsonConvert.SerializeObject(message));
            throw;
        }
    }

    private async Task ProcessSaveAnswerMessageAsync<T>(T message)
    {
        try
        {
            if (message is StudentAnswerSavedMessage saveAnswerMessage)
            {
                using var scope = _serviceScopeFactory.CreateScope();
            
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var studentExamSession = dbContext.StudentExamSessions
                    .SingleOrDefault(x => x.StudentExamSessionId == saveAnswerMessage.StudentExamSessionId);

                
                if (studentExamSession == null)
                {
                    _logger.LogWarning("❗ Không tìm thấy StudentExamSession. StudentExamSessionId: {StudentExamSessionId}", 
                        saveAnswerMessage.StudentExamSessionId);
                    return;
                }

                // Cập nhật đáp án mới
                studentExamSession.StudentAnswersString = saveAnswerMessage.NewAnswersString;
                await dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "📝 Đã cập nhật đáp án trực tiếp: StudentCode={StudentCode}, StudentExamSessionId={StudentExamSessionId}, Key={Key}, Value={Value}",
                    saveAnswerMessage.StudentCode, saveAnswerMessage.StudentExamSessionId, saveAnswerMessage.key, saveAnswerMessage.value
                );
            }
            else
            {
                _logger.LogWarning("⚠️ Message không phải là StudentAnswerSavedMessage: {MessageType}", typeof(T).Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi xử lý StudentAnswerSavedMessage trực tiếp: {Message}", JsonConvert.SerializeObject(message));
            throw;
        }
    }

    private async Task ProcessExamSubmissionMessageAsync<T>(T message)
    {
        try
        {
            if (message is ExamSubmissionMessage examSubmissionMessage)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Tìm StudentExamSession bằng StudentCode và ShuffledExamPaperId
                // Cần tìm chính xác phiên thi của sinh viên với đề thi cụ thể
                var studentExamSession = dbContext.StudentExamSessions
                    .FirstOrDefault(x => x.StudentCode == examSubmissionMessage.StudentCode && 
                                        x.ShuffledExamPaperId == examSubmissionMessage.ShuffledExamPaperId);

                if (studentExamSession == null)
                {
                    _logger.LogWarning("❗ Không tìm thấy StudentExamSession. StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}",
                        examSubmissionMessage.StudentCode, examSubmissionMessage.ShuffledExamPaperId);
                    return;
                }

                // Cập nhật thông tin nộp bài thi
                studentExamSession.EndTime = examSubmissionMessage.EndTime;
                studentExamSession.Score = examSubmissionMessage.Score;
                studentExamSession.CorrectAnswers = examSubmissionMessage.CorrectAnswers;
                studentExamSession.TotalQuestions = examSubmissionMessage.TotalQuestions;
                studentExamSession.IsCompleted = examSubmissionMessage.IsCompleted;
                studentExamSession.StudentAnswersString = examSubmissionMessage.StudentAnswersString;

                // Cập nhật audit fields
                studentExamSession.UpdatedAt = DateTime.UtcNow;
                studentExamSession.UpdatedBy = "system"; // Hoặc có thể lấy từ context

                await dbContext.SaveChangesAsync();

                // Gửi điểm số qua SignalR đến sinh viên cụ thể và tới monitor 
                try
                {
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();
                    
                    using var studentServiceScope = _serviceScopeFactory.CreateScope();
                    var studentService = studentServiceScope.ServiceProvider.GetRequiredService<IStudentService>();
                    
                    
                    var groupName1 = $"student_{examSubmissionMessage.StudentCode}";
                    
                    var groupName2 = $"lecturer_subject_{studentExamSession.ExamSessionSubjectId}";
                    
                    var scoreData = new
                    {
                        studentCode = examSubmissionMessage.StudentCode,
                        shuffledExamPaperId = examSubmissionMessage.ShuffledExamPaperId,
                        score = examSubmissionMessage.Score,
                        correctAnswers = examSubmissionMessage.CorrectAnswers,
                        totalQuestions = examSubmissionMessage.TotalQuestions,
                        startTime = studentExamSession.StartTime,
                        endTime = examSubmissionMessage.EndTime,
                        studentAnswersString = examSubmissionMessage.StudentAnswersString,
                        answerKey = studentExamSession.ShuffledExamPaper?.AnswerKey ?? "",
                        isCompleted = examSubmissionMessage.IsCompleted,
                        message = $"Bài thi đã được chấm điểm: {examSubmissionMessage.Score:F2}/10"
                    };
                    
                    //gửi tới sinh viên cụ thể
                    await hubContext.Clients.Group(groupName1).SendAsync("ReceiveExamScore", scoreData); 
                    
                    //gửi tới monitor lại danh sách sinh viên 
                    var (statusList, subjectInfo) = await studentService.GetStudentsByExamSessionSubjectAsync(studentExamSession.ExamSessionSubjectId);
                    await hubContext.Clients.Group(groupName2).SendAsync("RoomStatusUpdated", new StudentListResponse { 
                        Students = statusList.ToList(),
                        Subject = subjectInfo
                    });
                    
                    _logger.LogInformation("📤 Đã gửi điểm số qua SignalR đến sinh viên {StudentCode}. Điểm: {Score}", 
                        examSubmissionMessage.StudentCode, examSubmissionMessage.Score);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Lỗi khi gửi điểm số qua SignalR đến sinh viên {StudentCode}", 
                        examSubmissionMessage.StudentCode);
                }
            }
            else
            {
                _logger.LogWarning("⚠️ Message không phải là ExamSubmissionMessage: {MessageType}", typeof(T).Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi xử lý ExamSubmissionMessage trực tiếp: {Message}", JsonConvert.SerializeObject(message));
            throw;
        }
    }
    
} 