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

    public async Task<(bool Success, string Message, string? NewAnswersString)> UpdateSingleAnswerAsync(
        string studentCode, int shuffledExamPaperId, int index, string answer)
    {
        // Log thông tin
        _logger.LogInformation(
            "Đang lưu đáp án cho sinh viên. StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}, Index: {Index}, Answer: {Answer}",
            studentCode, shuffledExamPaperId, index, answer
        );

        try
        {
            // Lấy chuỗi đáp án hiện tại từ cache (có fallback về database)
            var currentAnswersString = await _sessionCacheHelper.GetStudentAnswersFromCacheAsync(studentCode, shuffledExamPaperId);
            
            if (string.IsNullOrEmpty(currentAnswersString))
            {
                _logger.LogError("Không tìm thấy chuỗi đáp án cho sinh viên {StudentCode} với ShuffledExamPaperId {ShuffledExamPaperId}", 
                    studentCode, shuffledExamPaperId);
                return (false, "Không tìm thấy bài thi của sinh viên", null);
            }

            // Tách chuỗi đáp án thành mảng
            var answerParts = currentAnswersString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var updatedParts = new List<string>();

            // Cập nhật đáp án tại index tương ứng
            bool found = false;
            foreach (var part in answerParts)
            {
                if (part.StartsWith($"({index},"))
                {
                    updatedParts.Add($"({index},{answer})");
                    found = true;
                }
                else if (!string.IsNullOrWhiteSpace(part))
                {
                    updatedParts.Add(part);
                }
            }

            // Nếu không tìm thấy index, thêm mới
            if (!found)
            {
                updatedParts.Add($"({index},{answer})");
            }

            // Tạo chuỗi đáp án mới
            string newAnswersString = string.Join(";", updatedParts) + ";";

            // Cập nhật vào cache (có fallback về database)
            var (success, message) = await _sessionCacheHelper.UpdateStudentAnswersInCacheAsync(
                studentCode, shuffledExamPaperId, newAnswersString);

            if (success)
            {
                _logger.LogInformation("Đã lưu đáp án thành công. Chuỗi đáp án mới: {NewAnswers}", newAnswersString);
                return (true, "Cập nhật đáp án thành công", newAnswersString);
            }
            else
            {
                _logger.LogError("Lỗi khi cập nhật đáp án: {Message}", message);
                return (false, message, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu đáp án cho sinh viên {StudentCode} với ShuffledExamPaperId {ShuffledExamPaperId}", 
                studentCode, shuffledExamPaperId);
            return (false, "Lỗi khi lưu đáp án", null);
        }
    }

    public async Task<(bool Success, string Message, Dictionary<int, string>? CurrentAnswers)> UpdateStudentAnswersOptimizedAsync(
        RedisValue studentAnswers, List<SaveAnswerDto> saveAnswerDtos, string studentCode, int shuffledExamPaperId)
    {
        if (!studentAnswers.HasValue)
        {
            _logger.LogError("Không tìm thấy bài làm của sinh viên trong cache");
            return (false, "Không tìm thấy bài làm của sinh viên", null);
        }
        
        var currentAnswers = ParseStudentAnswers(studentAnswers.ToString());
        
        // Batch update answers
        foreach (var answer in saveAnswerDtos)
        {
            if (currentAnswers.ContainsKey(answer.Index))
            {
                currentAnswers[answer.Index] = answer.Answer;
            }
        }
        
        var newAnswersString = CreateAnswersString(currentAnswers);
        
        // Cập nhật vào cache thay vì tạo key riêng
        var (success, message) = await _sessionCacheHelper.UpdateStudentAnswersInCacheAsync(
            studentCode, shuffledExamPaperId, newAnswersString);
        
        if (!success)
        {
            _logger.LogError("Lỗi khi cập nhật đáp án vào cache: {Message}", message);
            return (false, message, null);
        }
        
        return (true, "Cập nhật đáp án thành công", currentAnswers);
    }

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
    
    public string CreateAnswersString(Dictionary<int, string> answers)
    {
        return string.Join(";", answers.Select(pair => $"({pair.Key},{pair.Value})")) + ";";
    }

    public (double Score, int CorrectCount, int TotalQuestions) CalculateScoreOptimized(
        Dictionary<int, string> currentAnswers, Dictionary<int, string> correctAnswerPairs)
    {
        int correctCount = 0;
        int totalQuestions = correctAnswerPairs.Count;
        
        // Sử dụng LINQ để tối ưu performance
        correctCount = currentAnswers
            .Where(pair => correctAnswerPairs.TryGetValue(pair.Key, out string correctAnswer) && pair.Value == correctAnswer)
            .Count();
        
        double score = (double)correctCount / totalQuestions * 10;
        
        return (score, correctCount, totalQuestions);
    }

    public ExamSubmissionMessage CreateExamSubmissionMessage(string studentCode, int shuffledExamPaperId, 
        double score, int correctCount, int totalQuestions, Dictionary<int, string> currentAnswers)
    {
        var newAnswersString = CreateAnswersString(currentAnswers);
        
        var examSubmissionDto = new ExamSubmissionDto
        {
            StudentCode = studentCode,
            ShuffledExamPaperId = shuffledExamPaperId,
            Score = score,
            CorrectAnswers = correctCount,
            TotalQuestions = totalQuestions,
            EndTime = DateTimeHelper.GetVietnamTime(),
            StudentAnswersString = newAnswersString
        };
        
        return _mapper.Map<ExamSubmissionMessage>(examSubmissionDto);
    }
    
    public ExamSubmissionMessage CreateSaveExamMessage(string studentCode, int shuffledExamPaperId, Dictionary<int, string> currentAnswers)
    {
        var newAnswersString = CreateAnswersString(currentAnswers);
        
        var saveExamDto = new ExamSubmissionDto
        {
            StudentCode = studentCode,
            ShuffledExamPaperId = shuffledExamPaperId,
            Score = null,
            CorrectAnswers = null,
            TotalQuestions = null,
            EndTime = DateTimeHelper.GetVietnamTime(),
            StudentAnswersString = newAnswersString
        };
        
        return _mapper.Map<ExamSubmissionMessage>(saveExamDto);
    }

    public StudentAnswerSavedMessage CreateAnswerSavedMessage(string studentCode, int shuffledExamPaperId, int index, string answer, string newAnswersString)
    {
        return new StudentAnswerSavedMessage
        {
            StudentCode = studentCode,
            ShuffledExamPaperId = shuffledExamPaperId,
            Index = index,
            Answer = answer,
            NewAnswersString = newAnswersString
        };
    }
} 