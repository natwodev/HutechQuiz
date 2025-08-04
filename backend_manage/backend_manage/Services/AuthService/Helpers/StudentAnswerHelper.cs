using backend_manage.DTOs;
using backend_manage.Messages;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using AutoMapper;
using backend_manage.Extensions;
using backend_manage.Hubs;
using backend_manage.Messages.RabbitMQ;
using System.Text.RegularExpressions;

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
            
            // Parse chuỗi đáp án hiện tại thành Dictionary
            var answersDict = ParseStudentAnswers(currentAnswersString);
            
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
                newAnswersString = UpdateRegularQuestionAnswer(currentAnswersString, saveAnswerDto.Index, saveAnswerDto.Answer);
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

    private string UpdateRegularQuestionAnswer(string currentAnswersString, int index, string answer)
    {
        var answersDict = ParseStudentAnswers(currentAnswersString);
        answersDict[index] = answer;
        return CreateAnswersString(answersDict);
    }

    private string UpdateGroupQuestionAnswer(string currentAnswersString, int index, int subIndex, string answer)
    {
        // Tìm câu hỏi nhóm hiện tại
        var groupQuestionPattern = $@"\({index},\([^)]+\)\)";
        var match = System.Text.RegularExpressions.Regex.Match(currentAnswersString, groupQuestionPattern);
        
        if (match.Success)
        {
            // Cập nhật câu hỏi nhóm hiện có
            var groupContent = match.Value;
            var newGroupContent = UpdateSubAnswerInGroup(groupContent, subIndex, answer);
            return currentAnswersString.Replace(groupContent, newGroupContent);
        }
        else
        {
            // Tạo câu hỏi nhóm mới
            var newGroupQuestion = $"({index},({subIndex},{answer}))";
            
            if (string.IsNullOrEmpty(currentAnswersString))
            {
                return newGroupQuestion;
            }
            else
            {
                return currentAnswersString + ";" + newGroupQuestion;
            }
        }
    }

    private string UpdateSubAnswerInGroup(string groupContent, int subIndex, string answer)
    {
        // Parse nội dung nhóm: (index,(subindex1,answer1);(subindex2,answer2);...)
        var innerContent = groupContent.Substring(groupContent.IndexOf('(') + 1, groupContent.LastIndexOf(')') - groupContent.IndexOf('(') - 1);
        var parts = innerContent.Split(',', 2);
        var mainIndex = parts[0];
        var subAnswers = parts[1].Trim('(', ')');
        
        // Parse các câu hỏi con
        var subAnswerDict = new Dictionary<int, string>();
        var subParts = subAnswers.Split(';', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in subParts)
        {
            var cleanPart = part.Trim('(', ')');
            var subParts2 = cleanPart.Split(',');
            if (subParts2.Length == 2)
            {
                subAnswerDict[int.Parse(subParts2[0])] = subParts2[1];
            }
        }
        
        // Cập nhật câu hỏi con
        subAnswerDict[subIndex] = answer;
        
        // Tạo lại chuỗi câu hỏi con
        var newSubAnswers = string.Join(";", subAnswerDict.Select(kv => $"({kv.Key},{kv.Value})"));
        
        return $"({mainIndex},({newSubAnswers}))";
    }

    public Dictionary<int, string> ParseStudentAnswers(string answersString)
    {
        if (string.IsNullOrEmpty(answersString))
            return new Dictionary<int, string>();

        try
        {
            var result = new Dictionary<int, string>();
            var parts = answersString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var part in parts)
            {
                var cleanPart = part.Trim('(', ')');
                var subParts = cleanPart.Split(',', 2);
                
                if (subParts.Length == 2)
                {
                    var index = int.Parse(subParts[0]);
                    var answer = subParts[1];
                    
                    // Kiểm tra xem có phải câu hỏi nhóm không
                    if (answer.StartsWith("(") && answer.EndsWith(")"))
                    {
                        // Đây là câu hỏi nhóm, lưu nguyên chuỗi
                        result[index] = answer;
                    }
                    else
                    {
                        // Câu hỏi thường
                        result[index] = answer;
                    }
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Lỗi khi parse chuỗi đáp án: {AnswersString}. Lỗi: {Error}", answersString, ex.Message);
            return new Dictionary<int, string>();
        }
    }

    public string CreateAnswersString(Dictionary<int, string> answers)
    {
        if (answers == null || answers.Count == 0)
            return "";
            
        return string.Join(";", answers.Select(kv => $"({kv.Key},{kv.Value})"));
    }
} 