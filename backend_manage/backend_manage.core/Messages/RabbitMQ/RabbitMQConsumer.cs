using backend_manage.core.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace backend_manage.core.Messages.RabbitMQ
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly IRabbitMqService _rabbitMqService;
        private readonly ILogger<RabbitMqConsumer> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private const int StartExamConsumerCount = 2; // 2 consumers cho bắt đầu làm bài
        private const int SaveAnswerConsumerCount = 3; // 3 consumers cho lưu đáp án
        private const int ExamSubmissionConsumerCount = 2; // 2 consumers cho nộp bài thi
        private const string StartExamQueue = "start_exam_queue";
        private const string SaveAnswerQueue = "save_answer_queue";
        private const string ExamSubmissionQueue = "exam_submission_queue";

        public RabbitMqConsumer(
            IRabbitMqService rabbitMqService,
            ILogger<RabbitMqConsumer> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _rabbitMqService = rabbitMqService;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Khởi tạo consumers cho start_exam_queue
            for (int i = 0; i < StartExamConsumerCount; i++)
            {
                int consumerId = i + 1;

                _rabbitMqService.Subscribe<StartExamMessage>(StartExamQueue, async (message) =>
                {
                    try
                    {
                        _logger.LogInformation("🕒 Consumer {ConsumerId} bắt đầu xử lý: {Time}", consumerId, DateTime.UtcNow);
                        await ProcessStartExam(message);
                        _logger.LogInformation("✅ Consumer {ConsumerId} hoàn tất xử lý: {Time}", consumerId, DateTime.UtcNow);
                        _logger.LogInformation("✅ Consumer {ConsumerId} đã xử lý xong message từ queue: {QueueName}",
                            consumerId, StartExamQueue);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Lỗi khi xử lý message bởi Consumer {ConsumerId} - Queue: {QueueName}",
                            consumerId, StartExamQueue);
                    }
                });

                _logger.LogInformation("🚀 Bắt đầu Consumer {ConsumerId} cho queue: {QueueName}", consumerId, StartExamQueue);
            }

            // Khởi tạo consumers cho save_answer_queue
            for (int i = 0; i < SaveAnswerConsumerCount; i++)
            {
                int consumerId = i + 1;

                _rabbitMqService.Subscribe<StudentAnswerSavedMessage>(SaveAnswerQueue, async (message) =>
                {
                    try
                    {
                        _logger.LogInformation("🕒 SaveAnswer Consumer {ConsumerId} bắt đầu xử lý: {Time}", consumerId, DateTime.UtcNow);
                        await ProcessSaveAnswer(message);
                        _logger.LogInformation("✅ SaveAnswer Consumer {ConsumerId} hoàn tất xử lý: {Time}", consumerId, DateTime.UtcNow);
                        _logger.LogInformation("✅ SaveAnswer Consumer {ConsumerId} đã xử lý xong message từ queue: {QueueName}",
                            consumerId, SaveAnswerQueue);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Lỗi khi xử lý message bởi SaveAnswer Consumer {ConsumerId} - Queue: {QueueName}",
                            consumerId, SaveAnswerQueue);
                    }
                });

                _logger.LogInformation("🚀 Bắt đầu SaveAnswer Consumer {ConsumerId} cho queue: {QueueName}", consumerId, SaveAnswerQueue);
            }

            // Khởi tạo consumers cho exam_submission_queue
            for (int i = 0; i < ExamSubmissionConsumerCount; i++)
            {
                int consumerId = i + 1;

                _rabbitMqService.Subscribe<ExamSubmissionMessage>(ExamSubmissionQueue, async (message) =>
                {
                    try
                    {
                        _logger.LogInformation("🕒 ExamSubmission Consumer {ConsumerId} bắt đầu xử lý: {Time}", consumerId, DateTime.UtcNow);
                        await ProcessExamSubmission(message);
                        _logger.LogInformation("✅ ExamSubmission Consumer {ConsumerId} hoàn tất xử lý: {Time}", consumerId, DateTime.UtcNow);
                        _logger.LogInformation("✅ ExamSubmission Consumer {ConsumerId} đã xử lý xong message từ queue: {QueueName}",
                            consumerId, ExamSubmissionQueue);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Lỗi khi xử lý message bởi ExamSubmission Consumer {ConsumerId} - Queue: {QueueName}",
                            consumerId, ExamSubmissionQueue);
                    }
                });

                _logger.LogInformation("🚀 Bắt đầu ExamSubmission Consumer {ConsumerId} cho queue: {QueueName}", consumerId, ExamSubmissionQueue);
            }

            return Task.CompletedTask;
        }

        private async Task ProcessStartExam(StartExamMessage message)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var studentExamSession = dbContext.StudentExamSessions
                    .FirstOrDefault(x => x.StudentExamSessionId == message.StudentExamSessionId
                        && x.StudentCode == message.StudentCode);

                if (studentExamSession == null)
                {
                    _logger.LogWarning("❗ Không tìm thấy StudentExamSession. StudentExamSessionId: {StudentExamSessionId}, StudentCode: {StudentCode}",
                        message.StudentExamSessionId, message.StudentCode);
                    return;
                }

                // Cập nhật thông tin bắt đầu làm bài
                studentExamSession.StartTime = message.StartTime;
                studentExamSession.ShuffledExamPaperId = message.ShuffledExamPaperId;
                studentExamSession.StudentAnswersString = message.StudentAnswersString;
                studentExamSession.IsCompleted = message.IsCompleted;

                await dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "📝 Đã cập nhật bắt đầu làm bài: StudentCode={StudentCode}, ExamSessionId={StudentExamSessionId}, PaperId={ShuffledExamPaperId}, StartTime={StartTime}",
                    message.StudentCode, message.StudentExamSessionId, message.ShuffledExamPaperId, message.StartTime
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi xử lý StartExamMessage: StudentCode={StudentCode}, ExamSessionId={StudentExamSessionId}",
                    message.StudentCode, message.StudentExamSessionId);
                throw;
            }
        }

        private async Task ProcessSaveAnswer(StudentAnswerSavedMessage message)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Tìm StudentExamSession bằng StudentExamSessionId
                var studentExamSession = dbContext.StudentExamSessions
                    .SingleOrDefault(x => x.StudentExamSessionId == message.StudentExamSessionId);

                if (studentExamSession == null)
                {
                    _logger.LogWarning("❗ Không tìm thấy StudentExamSession. StudentExamSessionId: {StudentExamSessionId}", message.StudentExamSessionId);
                    return; 
                }

                // Cập nhật đáp án mới
                studentExamSession.StudentAnswersString = message.NewAnswersString;
                await dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "📝 Đã cập nhật đáp án vào database: StudentCode={StudentCode}, StudentExamSessionId={StudentExamSessionId}, Index={Index}, Answer={Answer}",
                    message.StudentCode, message.StudentExamSessionId, message.Index, message.Answer
                );

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi xử lý StudentAnswerSavedMessage: StudentCode={StudentCode}, Index={Index}, Answer={Answer}",
                    message.StudentCode, message.Index, message.Answer);
                throw;
            }
        }

        private async Task ProcessExamSubmission(ExamSubmissionMessage message)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Tìm StudentExamSession bằng StudentCode và ShuffledExamPaperId
                var studentExamSession = dbContext.StudentExamSessions
                    .FirstOrDefault(x => x.StudentCode == message.StudentCode && 
                                        x.ShuffledExamPaperId == message.ShuffledExamPaperId);

                if (studentExamSession == null)
                {
                    _logger.LogWarning("❗ Không tìm thấy StudentExamSession. StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}",
                        message.StudentCode, message.ShuffledExamPaperId);
                    return;
                }

                // Cập nhật thông tin nộp bài thi
                studentExamSession.EndTime = message.EndTime;
                studentExamSession.Score = message.Score;
                studentExamSession.CorrectAnswers = message.CorrectAnswers;
                studentExamSession.TotalQuestions = message.TotalQuestions;
                studentExamSession.IsCompleted = message.IsCompleted;
                studentExamSession.StudentAnswersString = message.StudentAnswersString;

                // Cập nhật audit fields
                studentExamSession.UpdatedAt = DateTime.UtcNow;
                studentExamSession.UpdatedBy = "system";

                await dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "📝 Đã cập nhật nộp bài thi vào database: StudentCode={StudentCode}, ShuffledExamPaperId={ShuffledExamPaperId}, Score={Score}, CorrectAnswers={CorrectAnswers}/{TotalQuestions}, EndTime={EndTime}",
                    message.StudentCode, 
                    message.ShuffledExamPaperId, 
                    message.Score, 
                    message.CorrectAnswers, 
                    message.TotalQuestions,
                    message.EndTime
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi xử lý ExamSubmissionMessage: StudentCode={StudentCode}, ShuffledExamPaperId={ShuffledExamPaperId}",
                    message.StudentCode, message.ShuffledExamPaperId);
                throw;
            }
        }
    }
}
