using backend_manage.core.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using backend_manage.core.Hubs;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.SignalR;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace backend_manage.core.Messages.RabbitMQ
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly IRabbitMqService _rabbitMqService;
        private readonly ILogger<RabbitMqConsumer> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IHubContext<NotificationHub> _hubContext;
        private bool _consumersRegistered = false;
        private readonly object _lockObject = new object();

        private const int StartExamConsumerCount = 2;
        private const int SaveAnswerConsumerCount = 3;
        private const int ExamSubmissionConsumerCount = 2;
        private const string StartExamQueue = "start_exam_queue";
        private const string SaveAnswerQueue = "save_answer_queue";
        private const string ExamSubmissionQueue = "exam_submission_queue";

        public RabbitMqConsumer(
            IRabbitMqService rabbitMqService,
            ILogger<RabbitMqConsumer> logger,
            IServiceScopeFactory serviceScopeFactory,
            IHubContext<NotificationHub> hubContext)
        {
            _rabbitMqService = rabbitMqService;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Đăng ký consumers lần đầu
            RegisterAllConsumers();

            // Timer để kiểm tra và re-register consumer khi cần
            var timer = new Timer(async _ => await CheckAndReRegisterConsumers(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

            // Chờ cho đến khi service bị dừng
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }

            timer.Dispose();
        }

        private async Task CheckAndReRegisterConsumers()
        {
            try
            {
                // Kiểm tra nếu RabbitMQ đã kết nối nhưng consumer chưa được đăng ký
                if (_rabbitMqService.IsConnected && !_consumersRegistered)
                {
                    _logger.LogInformation("🔄 RabbitMQ đã kết nối lại, đang re-register consumers...");
                    RegisterAllConsumers();
                }
                // Kiểm tra nếu RabbitMQ không kết nối và consumer đã được đăng ký
                else if (!_rabbitMqService.IsConnected && _consumersRegistered)
                {
                    _logger.LogWarning("⚠️ RabbitMQ mất kết nối, đánh dấu consumers chưa được đăng ký");
                    lock (_lockObject)
                    {
                        _consumersRegistered = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi kiểm tra và re-register consumers");
            }
        }

        private void RegisterAllConsumers()
        {
            lock (_lockObject)
            {
                if (_consumersRegistered)
                {
                    return;
                }

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
                            // Delay 10 giây theo yêu cầu trước khi xử lý nộp bài
                            await Task.Delay(TimeSpan.FromSeconds(10));
                            await ProcessExamSubmission(message);
                            _logger.LogInformation("✅ ExamSubmission Consumer {ConsumerId} hoàn tất xử lý: {Time}", consumerId, DateTime.UtcNow);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Lỗi khi xử lý message bởi ExamSubmission Consumer {ConsumerId} - Queue: {QueueName}",
                                consumerId, ExamSubmissionQueue);
                        }
                    });

                    _logger.LogInformation("🚀 Bắt đầu ExamSubmission Consumer {ConsumerId} cho queue: {QueueName}", consumerId, ExamSubmissionQueue);
                }

                _consumersRegistered = true;
                _logger.LogInformation("✅ Đã đăng ký tất cả consumers thành công");
            }
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
            studentExamSession.OriginalExamPaperId = message.OriginalExamPaperId;
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
                    "📝 Đã cập nhật đáp án vào database: StudentCode={StudentCode}, StudentExamSessionId={StudentExamSessionId}, Key={Key}, Value={Value}",
                    message.StudentCode, message.StudentExamSessionId, message.key, message.value
                );

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi xử lý StudentAnswerSavedMessage: StudentCode={StudentCode}, Key={Key}, Value={Value}",
                    message.StudentCode, message.key, message.value);
                throw;
            }
        }

        private async Task ProcessExamSubmission(ExamSubmissionMessage message)
        {
            try
            {
                await Task.Delay(10000);
                
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

                // Gửi điểm qua SignalR tới sinh viên và cập nhật monitor
                try
                {
                    var studentService = scope.ServiceProvider.GetRequiredService<IStudentService>();

                    var groupNameStudent = $"student_{message.StudentCode}";
                    var groupNameLecturer = $"lecturer_subject_{studentExamSession.ExamSessionSubjectId}";

                    var scoreData = new
                    {
                        studentCode = message.StudentCode,
                        shuffledExamPaperId = message.ShuffledExamPaperId,
                        score = message.Score,
                        correctAnswers = message.CorrectAnswers,
                        totalQuestions = message.TotalQuestions,
                        startTime = studentExamSession.StartTime,
                        endTime = message.EndTime,
                        studentAnswersString = message.StudentAnswersString,
                        answerKey = studentExamSession.ShuffledExamPaper?.AnswerKey ?? string.Empty,
                        isCompleted = message.IsCompleted,
                        message = $"Bài thi đã được chấm điểm: {message.Score:F2}/10"
                    };

                    await _hubContext.Clients.Group(groupNameStudent).SendAsync("ReceiveExamScore", scoreData);

                    var (statusList, subjectInfo) = await studentService.GetStudentsByExamSessionSubjectAsync(studentExamSession.ExamSessionSubjectId);
                    await _hubContext.Clients.Group(groupNameLecturer).SendAsync("RoomStatusUpdated", new StudentListResponse
                    {
                        Students = statusList.ToList(),
                        Subject = subjectInfo
                    });

                    _logger.LogInformation("📤 Đã gửi điểm qua SignalR: Student={StudentCode}, Score={Score}", message.StudentCode, message.Score);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Lỗi khi gửi điểm qua SignalR cho StudentCode={StudentCode}", message.StudentCode);
                }
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
