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
        private const string ExamSubmissionQueue = "submit_exam_queue";
        private const string StudentAnswerSavedQueue = "save_answer_queue";
        private const string SaveExamQueue = "save_exam_queue";
        private const string CacheStudentExamSessionsQueue = "cache_student_exam_sessions_queue";
        
        // Cấu hình số lượng consumers cho xử lý song song
        private const int StudentAnswerConsumerCount = 2; // 2 consumers cho lưu đáp án
        private const int ExamSubmissionConsumerCount = 2; // 2 consumers cho nộp bài
        private const int SaveExamConsumerCount = 2; // 2 consumers cho lưu bài
        private const int CacheStudentExamSessionsConsumerCount = 1; // Số lượng consumer cho queue này

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
            // Tạo nhiều consumers cho student_answer_saved_queue
            for (int i = 0; i < StudentAnswerConsumerCount; i++)
            {
                _rabbitMQService.Subscribe<StudentAnswerSavedMessage>(StudentAnswerSavedQueue, ProcessStudentAnswerSaved);
                _logger.LogInformation("Bắt đầu consumer {ConsumerId} cho queue: {QueueName}", i + 1, StudentAnswerSavedQueue);
            }
            
            // Tạo nhiều consumers cho exam_submission_queue
            for (int i = 0; i < ExamSubmissionConsumerCount; i++)
            {
                _rabbitMQService.Subscribe<ExamSubmissionMessage>(ExamSubmissionQueue, ProcessExamSubmission);
                _logger.LogInformation("Bắt đầu consumer {ConsumerId} cho queue: {QueueName}", i + 1, ExamSubmissionQueue);
            }
            
            // Tạo nhiều consumers cho save_exam_queue
            for (int i = 0; i < SaveExamConsumerCount; i++)
            {
                _rabbitMQService.Subscribe<ExamSubmissionMessage>(SaveExamQueue, ProcessSaveExam);
                _logger.LogInformation("Bắt đầu consumer {ConsumerId} cho queue: {QueueName}", i + 1, SaveExamQueue);
            }
            
            // Thêm consumer cho cache_student_exam_sessions_queue
            for (int i = 0; i < CacheStudentExamSessionsConsumerCount; i++)
            {
                _rabbitMQService.Subscribe<CacheStudentExamSessionsMessage>(CacheStudentExamSessionsQueue, ProcessCacheStudentExamSessions);
                _logger.LogInformation("Bắt đầu consumer {ConsumerId} cho queue: {QueueName}", i + 1, CacheStudentExamSessionsQueue);
            }
            
            _logger.LogInformation("Đã khởi tạo {StudentAnswerCount} consumers cho lưu đáp án, {ExamSubmissionCount} consumers cho nộp bài và {SaveExamCount} consumers cho lưu bài", 
                StudentAnswerConsumerCount, ExamSubmissionConsumerCount, SaveExamConsumerCount);
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
                        "Không tìm thấy StudentExamSession. StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}",
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

                // Đồng bộ cache IsCompleted vào Redis
                try
                {
                    var redis = new StackExchange.Redis.ConnectionMultiplexer[] { };
                    if (scope.ServiceProvider.GetService(typeof(StackExchange.Redis.IConnectionMultiplexer)) is StackExchange.Redis.IConnectionMultiplexer redisConn)
                    {
                        var redisDb = redisConn.GetDatabase();
                        var cacheKey = $"student_exam_session_completed:{message.StudentCode}:{message.ShuffledExamPaperId}";
                        redisDb.StringSet(cacheKey, message.IsCompleted ? "1" : "0", TimeSpan.FromHours(6));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi đồng bộ cache IsCompleted vào Redis");
                }

                _logger.LogInformation(
                    "Đã lưu kết quả bài thi vào DB. StudentCode: {StudentCode}, Điểm: {Score}",
                    message.StudentCode,
                    message.Score
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu kết quả bài thi vào DB");
                throw;
            }
        }

        private void ProcessSaveExam(ExamSubmissionMessage message)
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
                        "Không tìm thấy StudentExamSession (lưu bài). StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}",
                        message.StudentCode,
                        message.ShuffledExamPaperId
                    );
                    return;
                }

                // Chỉ cập nhật đáp án và thời gian, không set điểm hoặc trạng thái hoàn thành
                studentExamSession.StudentAnswersString = message.StudentAnswersString;
                studentExamSession.EndTime = message.EndTime;
                // Không cập nhật Score, CorrectAnswers, TotalQuestions, IsCompleted

                dbContext.SaveChanges();
                _logger.LogInformation(
                    "Đã lưu bài nháp vào DB. StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}",
                    message.StudentCode,
                    message.ShuffledExamPaperId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu bài nháp vào DB");
                throw;
            }
        }

        private void ProcessStudentAnswerSaved(StudentAnswerSavedMessage message)
        {
            try
            {
                _logger.LogInformation(
                    "Đã nhận message lưu đáp án. StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}, Index: {Index}, Answer: {Answer}",
                    message.StudentCode, message.ShuffledExamPaperId, message.Index, message.Answer
                );

                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Tìm StudentExamSession
                var studentExamSession = dbContext.StudentExamSessions
                    .FirstOrDefault(x => x.StudentCode == message.StudentCode
                        && x.ShuffledExamPaperId == message.ShuffledExamPaperId);

                if (studentExamSession == null)
                {
                    _logger.LogWarning(
                        "Không tìm thấy StudentExamSession. StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}",
                        message.StudentCode, message.ShuffledExamPaperId
                    );
                    return;
                }

                // Cập nhật đáp án trong DB
                // Lưu ý: Đây chỉ là backup, đáp án chính vẫn ở Redis
                // Có thể thêm trường AnswerHistory hoặc tạo bảng riêng để lưu lịch sử đáp án
                
                // Sử dụng chuỗi đáp án đã tính sẵn từ service
                studentExamSession.StudentAnswersString = message.NewAnswersString;

                dbContext.SaveChanges();

                // Có thể thêm logic xử lý ở đây như:
                // - Log chi tiết việc lưu đáp án
                // - Gửi thông báo realtime
                // - Backup dữ liệu
                // - Phân tích hành vi sinh viên
                // - v.v.

                _logger.LogInformation(
                    "Đã lưu đáp án vào DB thành công. StudentCode: {StudentCode}, Index: {Index}, Answer: {Answer}",
                    message.StudentCode, message.Index, message.Answer
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý message lưu đáp án. StudentCode: {StudentCode}, Index: {Index}",
                    message.StudentCode, message.Index);
                throw;
            }
        }

        private void ProcessCacheStudentExamSessions(CacheStudentExamSessionsMessage message)
        {
            try
            {
                string studentCode = message.StudentCode;
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var redisConn = scope.ServiceProvider.GetService(typeof(StackExchange.Redis.IConnectionMultiplexer)) as StackExchange.Redis.IConnectionMultiplexer;
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<RabbitMqConsumer>>();

                if (redisConn == null)
                {
                    logger.LogError("Không lấy được Redis connection trong ProcessCacheStudentExamSessions");
                    return;
                }
                var redisDb = redisConn.GetDatabase();

                // 1. Truy vấn tất cả phiên thi của sinh viên
                var studentExamSessions = dbContext.StudentExamSessions
                    .Where(x => x.StudentCode == studentCode)
                    .ToList();

                foreach (var session in studentExamSessions)
                {
                    // 2. Cache vào Redis
                    string sessionCacheKey = $"student_exam_session:{studentCode}:{session.StudentExamSessionId}";
                    var jsonSession = System.Text.Json.JsonSerializer.Serialize(session);
                    redisDb.StringSet(sessionCacheKey, jsonSession, TimeSpan.FromHours(6));
                }
                logger.LogInformation("Đã cache StudentExamSession của sinh viên {StudentCode}", studentCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý cache_student_exam_sessions_queue");
            }
        }
    }
} 