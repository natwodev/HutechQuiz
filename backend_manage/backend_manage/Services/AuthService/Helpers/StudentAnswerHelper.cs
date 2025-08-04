using backend_manage.DTOs;
using backend_manage.Messages;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using AutoMapper;
using backend_manage.Extensions;
using backend_manage.Hubs;
using backend_manage.Messages.RabbitMQ;

namespace backend_manage.Services.AuthService.Helpers;

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

    public async Task<(bool Success, string Message, string? NewAnswersString)> UpdateSingleAnswerAsync(string studentCode, int studentExamSessionId, int index, string answer)
    {
        var (redisAvailable, studentExamSessionDto) = await _sessionCacheHelper.GetStudentExamSessionAsync(studentCode, studentExamSessionId);
        if (studentExamSessionDto == null)
        {
            _logger.LogWarning("Không tìm thấy phiên thi của sinh viên {StudentCode} với ID phiên thi {SessionId}", studentCode, studentExamSessionId);
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
            
            // Parse chuỗi đáp án hiện tại thành Dictionary
            var answersDict = ParseStudentAnswers(currentAnswersString);
            
            // Cập nhật đáp án tại vị trí index
            answersDict[index] = answer;
            
            // Tạo chuỗi đáp án mới
            string newAnswersString = string.Join(";", 
                answersDict.OrderBy(x => x.Key)
                          .Select(kvp => $"({kvp.Key},{kvp.Value})")) + ";";
            
            // Cập nhật vào DTO
            studentExamSessionDto.StudentAnswersString = newAnswersString;            
            // Lưu vào cache
            await _sessionCacheHelper.UpdateStudentExamSessionAsync(studentCode, studentExamSessionDto);
            
            // Gửi message qua RabbitMQ để lưu vào database
           var saveAnswerMessage = new StudentAnswerSavedMessage
           {
               StudentCode = studentCode,
               StudentExamSessionId = studentExamSessionId,
               Index = index,
               Answer = answer,
               NewAnswersString = newAnswersString
           };

           _rabbitMqService.Publish("save_answer_queue", saveAnswerMessage);
           _logger.LogInformation("✅ Đã gửi message lưu đáp án qua RabbitMQ cho sinh viên {StudentCode} tại vị trí {Index}", 
               studentCode, index);
           
            return (true, "Cập nhật đáp án thành công", newAnswersString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật đáp án cho sinh viên {StudentCode} tại vị trí {Index}", 
                studentCode, index);
            return (false, "Lỗi khi cập nhật đáp án", null);
        }
    }

  
    public Dictionary<int, string> ParseStudentAnswers(string answersString)
    {
        if (string.IsNullOrEmpty(answersString))
            return new Dictionary<int, string>();

        try
        {
            return answersString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim('(', ')').Split(','))
                .Where(parts => parts.Length == 2) // Đảm bảo có đủ 2 phần
                .GroupBy(parts => int.Parse(parts[0])) // Group theo key để xử lý duplicate
                .ToDictionary(g => g.Key, g => g.Last().Last()); // Lấy giá trị cuối cùng nếu có duplicate
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Lỗi khi parse chuỗi đáp án: {AnswersString}. Lỗi: {Error}", answersString, ex.Message);
            return new Dictionary<int, string>();
        }
    }

    public Dictionary<int, string> ParseAnswerKey(string answerKeyString)
    {
        if (string.IsNullOrEmpty(answerKeyString))
            return new Dictionary<int, string>();

        try
        {
            return answerKeyString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim('(', ')').Split(','))
                .Where(parts => parts.Length == 2) // Đảm bảo có đủ 2 phần
                .GroupBy(parts => int.Parse(parts[0])) // Group theo key để xử lý duplicate
                .ToDictionary(g => g.Key, g => g.Last().Last()); // Lấy giá trị cuối cùng nếu có duplicate
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Lỗi khi parse chuỗi đáp án key: {AnswerKeyString}. Lỗi: {Error}", answerKeyString, ex.Message);
            return new Dictionary<int, string>();
        }
    }
    
} 