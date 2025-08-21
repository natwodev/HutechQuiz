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

    public StudentAnswerHelper(
        IConnectionMultiplexer redis,
        ILogger<StudentAnswerHelper> logger,
        IMapper mapper,
        StudentExamSessionCacheHelper sessionCacheHelper,
        IRabbitMqService rabbitMqService)
    {
        _redis = redis;
        _logger = logger;
        _mapper = mapper;
        _sessionCacheHelper = sessionCacheHelper;
        _rabbitMqService = rabbitMqService;
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

           _rabbitMqService.Publish("save_answer_queue", saveAnswerMessage);
           _logger.LogInformation("✅ Đã gửi message lưu đáp án qua RabbitMQ cho sinh viên {StudentCode} tại vị trí {Key}:{Value}", 
               studentCode, saveAnswerDto.key, saveAnswerDto.value);
           
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
    public async Task<(bool Success, string Message, string? NewAnswersString)> UpdateSingleAnswerAsync(string studentCode, int studentExamSessionId, int key, int value)
    {
        var saveAnswerDto = new SaveAnswerDto
        {
            StudentExamSessionId = studentExamSessionId,
            key = key,
            value = value
            
        };
        
        return await UpdateSingleAnswerAsync(saveAnswerDto, studentCode);
    }

    private static string UpdateSingleQuestionAnswer(string currentAnswerString, int key, int value)
    {
        // Parse chuỗi đáp án hiện tại
        var answersDict = ParseAnswersString(currentAnswerString);
    
        // Cập nhật đáp án cho câu hỏi đơn
        answersDict[key.ToString()] = value;
    
        // Tạo lại chuỗi đáp án
        return CreateAnswersString(answersDict);
    }

    private static string CreateAnswersString(Dictionary<string, int> answers)
    {
        if (answers == null || answers.Count == 0)
            return "";
        
        // Format: (key:value)
        return string.Join(";", answers.Select(kv => $"({kv.Key}:{kv.Value})"));
    }

    private static Dictionary<string, int> ParseAnswersString(string answersString)
    {
        if (string.IsNullOrEmpty(answersString))
            return new Dictionary<string, int>();

        try
        {
            var result = new Dictionary<string, int>();
            var parts = answersString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        
            foreach (var part in parts)
            {
                var cleanPart = part.Trim('(', ')');
                var subParts = cleanPart.Split(':', 2);
            
                if (subParts.Length == 2)
                {
                    var key = subParts[0];
                    var answerStr = subParts[1];
                
                    // Nếu value là số thì parse, còn nếu là "-" thì mặc định -1
                    if (int.TryParse(answerStr, out int answer))
                        result[key] = answer;
                    else
                        result[key] = -1; // hoặc để 0 tùy bạn định nghĩa
                }
            }
        
            return result;
        }
        catch
        {
            return new Dictionary<string, int>();
        }
    }

    
    
} 