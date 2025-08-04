using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using backend_manage.Data;
using backend_manage.Messages;
using backend_manage.DTOs;
using backend_manage.Services.Interfaces;
using backend_manage.Services.AuthService.Helpers;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace backend_manage.Messages.RabbitMQ
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly IRabbitMqService _rabbitMqService;
        private readonly ILogger<RabbitMqConsumer> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private const int StartExamConsumerCount = 2; // 2 consumers cho bắt đầu làm bài
        private const int SaveAnswerConsumerCount = 3; // 3 consumers cho lưu đáp án
        private const string StartExamQueue = "start_exam_queue";
        private const string SaveAnswerQueue = "save_answer_queue";

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
    }
}
