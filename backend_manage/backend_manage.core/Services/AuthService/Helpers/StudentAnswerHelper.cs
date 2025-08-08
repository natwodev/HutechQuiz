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
            
            // Cập nhật đáp án
            string newAnswersString;
            if (saveAnswerDto.SubIndex.HasValue)
            {
                // Cập nhật câu hỏi con trong câu hỏi nhóm
                newAnswersString = UpdateGroupQuestionAnswer(currentAnswersString, saveAnswerDto.Index, saveAnswerDto.SubIndex.Value, saveAnswerDto.Answer);
            }
            else
            {
                // Cập nhật câu hỏi thường
                newAnswersString = UpdateSingleQuestionAnswer(currentAnswersString, saveAnswerDto.Index, saveAnswerDto.Answer);
            }
         
            // Cập nhật vào DTO
            studentExamSessionDto.StudentAnswersString = newAnswersString;            
            // Lưu vào cache
            await _sessionCacheHelper.UpdateStudentExamSessionAsync(studentCode, studentExamSessionDto);
            
            // Gửi message qua RabbitMQ để lưu vào database
           var saveAnswerMessage = new StudentAnswerSavedMessage
           {
               StudentCode = studentCode,
               StudentExamSessionId = saveAnswerDto.StudentExamSessionId,
               Index = saveAnswerDto.Index,
               SubIndex = saveAnswerDto.SubIndex,
               Answer = saveAnswerDto.Answer,
               NewAnswersString = newAnswersString
           };

           _rabbitMqService.Publish("save_answer_queue", saveAnswerMessage);
           _logger.LogInformation("✅ Đã gửi message lưu đáp án qua RabbitMQ cho sinh viên {StudentCode} tại vị trí {Index},{SubIndex}", 
               studentCode, saveAnswerDto.Index, saveAnswerDto.SubIndex);
           
            return (true, "Cập nhật đáp án thành công", newAnswersString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật đáp án cho sinh viên {StudentCode} tại vị trí {Index}", 
                studentCode, saveAnswerDto.Index);
            return (false, "Lỗi khi cập nhật đáp án", null);
        }
    }

    // Overload method để tương thích với code cũ
    public async Task<(bool Success, string Message, string? NewAnswersString)> UpdateSingleAnswerAsync(string studentCode, int studentExamSessionId, int index, int? subIndex, string answer)
    {
        var saveAnswerDto = new SaveAnswerDto
        {
            StudentExamSessionId = studentExamSessionId,
            Index = index,
            SubIndex = subIndex,
            Answer = answer
        };
        
        return await UpdateSingleAnswerAsync(saveAnswerDto, studentCode);
    }

    private static string UpdateSingleQuestionAnswer(string currentAnswerString, int index, string answer)
    {
        // Parse chuỗi đáp án hiện tại
        var answersDict = ParseAnswersString(currentAnswerString);
        
        // Cập nhật đáp án cho câu hỏi đơn
        answersDict[index.ToString()] = answer;
        
        // Tạo lại chuỗi đáp án
        return CreateAnswersString(answersDict);
    }

    
    private static string UpdateGroupQuestionAnswer(string currentAnswerString, int index, int subindex, string answer)
    {
        // Parse chuỗi đáp án hiện tại
        var answersDict = ParseAnswersString(currentAnswerString);
        
        // Tạo key cho câu hỏi nhóm: index.subindex
        var groupKey = $"{index}.{subindex}";
        
        // Cập nhật đáp án cho câu hỏi nhóm
        answersDict[groupKey] = answer;
        
        // Tạo lại chuỗi đáp án
        return CreateAnswersString(answersDict);
    }

    private static string CreateAnswersString(Dictionary<string, string> answers)
    {
        if (answers == null || answers.Count == 0)
            return "";
            
        return string.Join(";", answers.Select(kv => $"({kv.Key},{kv.Value})"));
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
                var subParts = cleanPart.Split(',', 2);
                
                if (subParts.Length == 2)
                {
                    var key = subParts[0];
                    var answer = subParts[1];
                    result[key] = answer;
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            // Log error if needed
            return new Dictionary<string, string>();
        }
    }

    
    
} 