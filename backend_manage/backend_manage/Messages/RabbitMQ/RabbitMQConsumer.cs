using backend_manage.Data;
using backend_manage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using backend_manage.DTOs;
using backend_manage.Services.Interfaces;

namespace backend_manage.Messages.RabbitMQ
{
    public class RabbitMqFallbackConsumer : IRabbitMqConsumer
    {
        private readonly ILogger _logger;

        public RabbitMqFallbackConsumer(ILogger logger)
        {
            _logger = logger;
            _logger.LogWarning("Sử dụng RabbitMQ fallback consumer - RabbitMQ không khả dụng");
        }

        public void StartConsuming()
        {
            _logger.LogWarning("RabbitMQ không khả dụng - không thể start consumers");
        }
    }

    public class RabbitMqConsumer : IRabbitMqConsumer
    {
        private readonly IRabbitMqService _rabbitMqService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<RabbitMqConsumer> _logger;
        private const string ExamSubmissionQueue = "submit_exam_queue";
        private const string StudentAnswerSavedQueue = "save_answer_queue";
        private const string SaveExamQueue = "save_exam_queue";
        private const string StudentImportQueue = "student_import_queue";
        
        // Cấu hình số lượng consumers cho xử lý song song
        private const int StudentAnswerConsumerCount = 2; // 2 consumers cho lưu đáp án
        private const int ExamSubmissionConsumerCount = 2; // 2 consumers cho nộp bài
        private const int SaveExamConsumerCount = 2; // 2 consumers cho lưu bài
        private const int StudentImportConsumerCount = 2; // 1 consumer cho import (vì import nặng)

        public RabbitMqConsumer(
            IRabbitMqService rabbitMqService,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<RabbitMqConsumer> logger)
        {
            _rabbitMqService = rabbitMqService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public void StartConsuming()
        {
            try
            {
                // Tạo nhiều consumers cho student_answer_saved_queue
                for (int i = 0; i < StudentAnswerConsumerCount; i++)
                {
                    _rabbitMqService.Subscribe<StudentAnswerSavedMessage>(StudentAnswerSavedQueue, ProcessStudentAnswerSaved);
                    _logger.LogInformation("Bắt đầu consumer {ConsumerId} cho queue: {QueueName}", i + 1, StudentAnswerSavedQueue);
                }
                
                // Tạo nhiều consumers cho exam_submission_queue
                for (int i = 0; i < ExamSubmissionConsumerCount; i++)
                {
                    _rabbitMqService.Subscribe<ExamSubmissionMessage>(ExamSubmissionQueue, ProcessExamSubmission);
                    _logger.LogInformation("Bắt đầu consumer {ConsumerId} cho queue: {QueueName}", i + 1, ExamSubmissionQueue);
                }
                
                // Tạo nhiều consumers cho save_exam_queue
                for (int i = 0; i < SaveExamConsumerCount; i++)
                {
                    _rabbitMqService.Subscribe<ExamSubmissionMessage>(SaveExamQueue, ProcessSaveExam);
                    _logger.LogInformation("Bắt đầu consumer {ConsumerId} cho queue: {QueueName}", i + 1, SaveExamQueue);
                }
                
                // Tạo consumer cho student_import_queue
                for (int i = 0; i < StudentImportConsumerCount; i++)
                {
                    _rabbitMqService.Subscribe<StudentImportMessage>(StudentImportQueue, ProcessStudentImport);
                    _logger.LogInformation("Bắt đầu consumer {ConsumerId} cho queue: {QueueName}", i + 1, StudentImportQueue);
                }
                
                _logger.LogInformation("Đã khởi tạo {StudentAnswerCount} consumers cho lưu đáp án, {ExamSubmissionCount} consumers cho nộp bài, {SaveExamCount} consumers cho lưu bài và {StudentImportCount} consumer cho import", 
                    StudentAnswerConsumerCount, ExamSubmissionConsumerCount, SaveExamConsumerCount, StudentImportConsumerCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi khởi tạo RabbitMQ consumers");
                throw;
            }
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

        private async Task ProcessStudentImport(StudentImportMessage message)
        {
            try
            {
                _logger.LogInformation("Bắt đầu xử lý import student cho job {JobId}", message.JobId);
                
                using var scope = _serviceScopeFactory.CreateScope();
                var studentService = scope.ServiceProvider.GetRequiredService<IStudentService>();
                var redis = scope.ServiceProvider.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
                
                // 1. Update progress: 10% - Đang đọc file
                await UpdateImportProgress(redis, message.JobId, 10, "Processing", "Đang đọc file Excel...");
                
                // 2. Decode file content từ base64
                var fileBytes = Convert.FromBase64String(message.FileContent);
                using var stream = new MemoryStream(fileBytes);
                
                // 3. Update progress: 30% - Đang parse dữ liệu
                await UpdateImportProgress(redis, message.JobId, 30, "Processing", "Đang parse dữ liệu Excel...");
                
                // 4. Gọi service import (cần tạo method mới nhận Stream)
                var result = await studentService.ImportFromExcelStreamAsync(stream, message.ExamSessionSubjectCore, message.ExamRoomId, message.UserId);
                
                // 5. Update progress: 100% - Hoàn thành
                await UpdateImportProgress(redis, message.JobId, 100, "Completed", "Import thành công!", result);
                
                _logger.LogInformation("Hoàn thành import student cho job {JobId}. Kết quả: {StudentsAdded} sinh viên, {SessionsAdded} phiên thi", 
                    message.JobId, result.StudentsAdded, result.StudentExamSessionsAdded);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý import student cho job {JobId}", message.JobId);
                
                using var scope = _serviceScopeFactory.CreateScope();
                var redis = scope.ServiceProvider.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>();
                await UpdateImportProgress(redis, message.JobId, 0, "Failed", $"Lỗi: {ex.Message}");
            }
        }

        private async Task UpdateImportProgress(StackExchange.Redis.IConnectionMultiplexer redis, string jobId, int progress, string status, string message, StudentImportResultDto? result = null)
        {
            try
            {
                var progressData = new StudentImportProgressMessage
                {
                    JobId = jobId,
                    Progress = progress,
                    Status = status,
                    Message = message,
                    Result = result,
                    UpdatedAt = DateTime.UtcNow
                };
                
                var db = redis.GetDatabase();
                await db.StringSetAsync($"import_progress:{jobId}", System.Text.Json.JsonSerializer.Serialize(progressData), TimeSpan.FromHours(1));
                
                _logger.LogDebug("Đã cập nhật progress cho job {JobId}: {Progress}% - {Status}", jobId, progress, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật progress cho job {JobId}", jobId);
            }
        }
    }
} 