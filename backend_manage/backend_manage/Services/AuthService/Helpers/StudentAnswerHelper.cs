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

    public StudentAnswerHelper(
        IConnectionMultiplexer redis,
        ILogger<StudentAnswerHelper> logger,
        IMapper mapper,
        StudentExamSessionCacheHelper sessionCacheHelper)
    {
        _redis = redis;
        _logger = logger;
        _mapper = mapper;
        _sessionCacheHelper = sessionCacheHelper;
    }
/*
    public async Task<(bool Success, string Message, string? NewAnswersString)> UpdateSingleAnswerAsync(string studentCode, int studentExamSessionId, double index, string answer)
    {
        var (redisAvailable, studentExamSessionDto) = await _sessionCacheHelper.GetStudentExamSessionAsync(studentCode, studentExamSessionId);
        if (studentExamSessionDto == null)
        {
            _logger.LogWarning("Không tìm thấy phiên thi của sinh viên {StudentCode} với ID phiên thi {SessionId}", studentCode, studentExamSessionId);
            return (false,"Không có bài thi nào", null);
        }

        if (!studentExamSessionDto.ShuffledExamPaperId.HasValue)
        {
            string newAnswersString = studentExamSessionDto.StudentAnswersString;
            
            await _sessionCacheHelper.UpdateStudentExamSessionAsync(studentCode,studentExamSessionDto);
        }

    }
*/
  
    public Dictionary<int, string> ParseStudentAnswers(string answersString)
    {
        return answersString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim('(', ')').Split(','))
            .ToDictionary(parts => int.Parse(parts[0]), parts => parts[1]);
    }

    public Dictionary<int, string> ParseAnswerKey(string answerKeyString)
    {
        return answerKeyString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim('(', ')').Split(','))
            .ToDictionary(parts => int.Parse(parts[0]), parts => parts[1]);
    }
    
} 