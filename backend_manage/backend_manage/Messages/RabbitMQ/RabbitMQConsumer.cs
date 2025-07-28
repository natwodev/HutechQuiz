using backend_manage.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace backend_manage.Messages.RabbitMQ
{
    public class RabbitMqConsumer
    {
        private readonly IRabbitMqService _rabbitMQService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<RabbitMqConsumer> _logger;
        private const string ExamSubmissionQueue = "exam_submission_queue";

        public RabbitMqConsumer(
            IRabbitMqService rabbitMQService,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<RabbitMqConsumer> logger)
        {
            _rabbitMQService = rabbitMQService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public void StartConsuming()
        {
            _rabbitMQService.Subscribe<ExamSubmissionMessage>(ExamSubmissionQueue, ProcessExamSubmission);
            _logger.LogInformation("Started consuming messages from queue: {QueueName}", ExamSubmissionQueue);
        }

        private void ProcessExamSubmission(ExamSubmissionMessage message)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var studentExamSession = dbContext.StudentExamSessions
                    .FirstOrDefault(x => x.StudentCode == message.StudentCode
                        && x.ShuffledExamPaperId == message.ShuffledExamPaperId);

                if (studentExamSession == null)
                {
                    _logger.LogWarning(
                        "StudentExamSession not found. StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}",
                        message.StudentCode,
                        message.ShuffledExamPaperId
                    );
                    return;
                }

                studentExamSession.Score = message.Score;
                studentExamSession.CorrectAnswers = message.CorrectAnswers;
                studentExamSession.TotalQuestions = message.TotalQuestions;
                studentExamSession.IsCompleted = message.IsCompleted;
                studentExamSession.EndTime = message.EndTime;
                studentExamSession.StudentAnswersString = message.StudentAnswersString;

                dbContext.SaveChanges();
                _logger.LogInformation(
                    "Exam submission processed successfully. StudentCode: {StudentCode}, Score: {Score}",
                    message.StudentCode,
                    message.Score
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing exam submission message");
                throw;
            }
        }
    }
} 