using AutoMapper;
using backend_manage.core.Messages;
using backend_manage.core.Messages.RabbitMQ;
using backend_manage.shared.DTOs;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace backend_manage.core.Services.AuthService.Helpers;

public class StudentAnswerHelper
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<StudentAnswerHelper> _logger;
    private readonly IMapper _mapper;
    private readonly StudentExamSessionCacheHelper _sessionCacheHelper;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly IMessageProcessingService _messageProcessingService;

    public StudentAnswerHelper(
        IConnectionMultiplexer redis,
        ILogger<StudentAnswerHelper> logger,
        IMapper mapper,
        StudentExamSessionCacheHelper sessionCacheHelper,
        IRabbitMqService rabbitMqService,
        IMessageProcessingService messageProcessingService)
    {
        _redis = redis;
        _logger = logger;
        _mapper = mapper;
        _sessionCacheHelper = sessionCacheHelper;
        _rabbitMqService = rabbitMqService;
        _messageProcessingService = messageProcessingService;
    }

    public async Task<(bool Success, string Message, string? NewAnswersString)> UpdateSingleAnswerAsync(SaveAnswerDto saveAnswerDto, string studentCode)
    {
        var (redisAvailable, studentExamSessionDto) = await _sessionCacheHelper.GetStudentExamSessionAsync(studentCode, saveAnswerDto.StudentExamSessionId);
        if (studentExamSessionDto == null)
        {
            _logger.LogWarning("Không tìm thấy phiên thi của sinh viên {StudentCode} với ID phiên thi {SessionId}", studentCode, saveAnswerDto.StudentExamSessionId);
            return (false,"Không có bài thi nào", null);
        }

        if (!studentExamSessionDto.ShuffledExamPaperId.HasValue)
        {
            return (false, "Chưa có đề thi được phân công", null);
        }

        try
        {
            // Lấy chuỗi đáp án hiện tại
            string currentAnswersString = studentExamSessionDto.StudentAnswersString ?? "";
            
            var newAnswersString = UpdateSingleQuestionAnswer(currentAnswersString, saveAnswerDto.key, saveAnswerDto.value);

         
            // Cập nhật vào DTO
            studentExamSessionDto.StudentAnswersString = newAnswersString;            
            // Lưu vào cache
            await _sessionCacheHelper.UpdateStudentExamSessionAsync(studentCode, studentExamSessionDto);
            
            // Gửi message qua RabbitMQ để lưu vào database
           var saveAnswerMessage = new StudentAnswerSavedMessage
           {
               StudentCode = studentCode,
               StudentExamSessionId = saveAnswerDto.StudentExamSessionId,
               key = saveAnswerDto.key,
               value = saveAnswerDto.value,
               NewAnswersString = newAnswersString
           };

           try
           {
               _rabbitMqService.Publish("save_answer_queue", saveAnswerMessage);
               _logger.LogInformation("✅ Đã gửi message lưu đáp án qua RabbitMQ cho sinh viên {StudentCode} tại vị trí {Key}:{Value}", 
                   studentCode, saveAnswerDto.key, saveAnswerDto.value);
           }
           catch (Exception ex)
           {
               _logger.LogWarning(ex, "RabbitMQ publish thất bại, fallback xử lý trực tiếp save_answer_queue cho {StudentCode} tại {Key}:{Value}", 
                   studentCode, saveAnswerDto.key, saveAnswerDto.value);
               await _messageProcessingService.ProcessMessageAsync("save_answer_queue", saveAnswerMessage);
           }
           
            return (true, "Cập nhật đáp án thành công", newAnswersString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật đáp án cho sinh viên {StudentCode} tại vị trí {Key}", 
                studentCode, saveAnswerDto.key);
            return (false, "Lỗi khi cập nhật đáp án", null);
        }
    }

    // Overload method để tương thích với code cũ
    public async Task<(bool Success, string Message, string? NewAnswersString)> UpdateSingleAnswerAsync(string studentCode, int studentExamSessionId, int key, object value)
    {
        var saveAnswerDto = new SaveAnswerDto
        {
            StudentExamSessionId = studentExamSessionId,
            key = key,
            value = value
            
        };
        
        return await UpdateSingleAnswerAsync(saveAnswerDto, studentCode);
    }

    private static string UpdateSingleQuestionAnswer(string currentAnswerString, int key, object value)
    {
        // Parse chuỗi đáp án hiện tại
        var answersDict = ParseAnswersString(currentAnswerString);
    
        // Cập nhật đáp án cho câu hỏi đơn
        answersDict[key.ToString()] = value.ToString();
    
        // Tạo lại chuỗi đáp án
        return CreateAnswersString(answersDict);
    }

    private static string CreateAnswersString(Dictionary<string, string> answers)
    {
        if (answers == null || answers.Count == 0)
            return "";
        
        // Format: (key:value)
        return string.Join(";", answers.Select(kv => $"({kv.Key}:{kv.Value})"));
    }

    private static Dictionary<string, string> ParseAnswersString(string answersString)
    {
        if (string.IsNullOrEmpty(answersString))
            return new Dictionary<string, string>();

        try
        {
            var result = new Dictionary<string, string>();
            var parts = answersString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        
            foreach (var part in parts)
            {
                var cleanPart = part.Trim('(', ')');
                var subParts = cleanPart.Split(':', 2);
            
                if (subParts.Length == 2)
                {
                    var key = subParts[0];
                    var answerStr = subParts[1];
                
                    // Giữ nguyên giá trị gốc, không chuyển đổi
                    result[key] = answerStr;
                }
            }
        
            return result;
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    
    
} 