using backend_manage.Data;
using Newtonsoft.Json;

namespace backend_manage.Messages.RabbitMQ;

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
                    .FirstOrDefault(x => x.StudentExamSessionId == startExamMessage.StudentExamSessionId
                        && x.StudentCode == startExamMessage.StudentCode);

                if (studentExamSession == null)
                {
                    _logger.LogWarning("❗ Không tìm thấy StudentExamSession. StudentExamSessionId: {StudentExamSessionId}, StudentCode: {StudentCode}",
                        startExamMessage.StudentExamSessionId, startExamMessage.StudentCode);
                    return;
                }

                // Cập nhật thông tin bắt đầu làm bài
                studentExamSession.StartTime = startExamMessage.StartTime;
                studentExamSession.ShuffledExamPaperId = startExamMessage.ShuffledExamPaperId;
                studentExamSession.StudentAnswersString = startExamMessage.StudentAnswersString;
                studentExamSession.IsCompleted = startExamMessage.IsCompleted;

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

    // Thêm các method xử lý message khác ở đây
    // private async Task ProcessStudentImportMessageAsync<T>(T message)
    // {
    //     // Logic xử lý student import message
    // }
} 