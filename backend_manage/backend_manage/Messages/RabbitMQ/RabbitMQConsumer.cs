using backend_manage.Data;
using backend_manage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

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
        
        // Cấu hình số lượng consumers cho xử lý song song
        private const int StudentAnswerConsumerCount = 2; // 2 consumers cho lưu đáp án
        private const int ExamSubmissionConsumerCount = 2; // 2 consumers cho nộp bài
        private const int SaveExamConsumerCount = 2; // 2 consumers cho lưu bài

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
            
            _logger.LogInformation("Đã khởi tạo {StudentAnswerCount} consumers cho lưu đáp án, {ExamSubmissionCount} consumers cho nộp bài và {SaveExamCount} consumers cho lưu bài", 
                StudentAnswerConsumerCount, ExamSubmissionConsumerCount, SaveExamConsumerCount);
        }

        private async Task ProcessExamSubmission(ExamSubmissionMessage message)
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

                // Cập nhật IsCompleted trong cache StudentExamSession
                try
                {
                    if (scope.ServiceProvider.GetService(typeof(StackExchange.Redis.IConnectionMultiplexer)) is StackExchange.Redis.IConnectionMultiplexer redisConn)
                    {
                        var redisDb = redisConn.GetDatabase();
                        
                        // Tìm cache key của StudentExamSession với ShuffledExamPaperId cụ thể
                        var sessionCacheKey = $"student_exam_session:{message.StudentCode}:*";
                        var keys = redisDb.Multiplexer.GetServer(redisDb.Multiplexer.GetEndPoints().First()).Keys(pattern: sessionCacheKey);
                        
                        foreach (var key in keys)
                        {
                            try
                            {
                                // Đọc dữ liệu dưới dạng string (vì được lưu bằng StringSetAsync)
                                var sessionData = await redisDb.StringGetAsync(key);
                                if (sessionData.HasValue)
                                {
                                    var cachedStudentExamSession = System.Text.Json.JsonSerializer.Deserialize<StudentExamSession>(sessionData);
                                    if (cachedStudentExamSession != null && cachedStudentExamSession.ShuffledExamPaperId == message.ShuffledExamPaperId)
                                    {
                                        // Cập nhật IsCompleted trong cache StudentExamSession
                                        cachedStudentExamSession.IsCompleted = message.IsCompleted;
                                        var updatedSessionData = System.Text.Json.JsonSerializer.Serialize(cachedStudentExamSession);
                                        await redisDb.StringSetAsync(key, updatedSessionData, TimeSpan.FromHours(6));
                                        _logger.LogInformation("Đã cập nhật IsCompleted trong cache StudentExamSession: {Key} = {Value}", key, message.IsCompleted);
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Lỗi khi đọc/cập nhật StudentExamSession từ Redis cache với key: {Key}", key);
                                continue;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi cập nhật IsCompleted trong cache StudentExamSession");
                }

                _logger.LogInformation(
                    "Đã cập nhật kết quả bài thi vào DB. StudentCode: {StudentCode}, Score: {Score}, CorrectAnswers: {CorrectAnswers}/{TotalQuestions}",
                    message.StudentCode, message.Score, message.CorrectAnswers, message.TotalQuestions
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý message nộp bài thi. StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}",
                    message.StudentCode, message.ShuffledExamPaperId);
                throw;
            }
        }

        private async Task ProcessSaveExam(ExamSubmissionMessage message)
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
                        message.StudentCode, message.ShuffledExamPaperId
                    );
                    return;
                }

                // Chỉ cập nhật đáp án và thời gian, không set điểm hoặc trạng thái hoàn thành
                studentExamSession.StudentAnswersString = message.StudentAnswersString;
                studentExamSession.EndTime = message.EndTime;
                // Không cập nhật Score, CorrectAnswers, TotalQuestions, IsCompleted

                dbContext.SaveChanges();

                _logger.LogInformation(
                    "Đã lưu bài vào DB. StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}",
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

        private async Task ProcessStudentAnswerSaved(StudentAnswerSavedMessage message)
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
                        "Không tìm thấy StudentExamSession (lưu đáp án). StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}",
                        message.StudentCode,
                        message.ShuffledExamPaperId
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
    }
} 