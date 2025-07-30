using backend_manage.Entities;
using backend_manage.DTOs;
using backend_manage.Repositories;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using backend_manage.Repositories.Interfaces;

namespace backend_manage.Services.AuthService.Helpers;

public class ExamPaperHelper
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ExamPaperHelper> _logger;
    private readonly IRepository<ShuffledExamPaper> _shuffledExamPaperRepository;
    private readonly IRepository<ExamSessionSubject> _examSessionSubjectRepository;
    private readonly AutoMapper.IMapper _mapper;

    public ExamPaperHelper(
        IConnectionMultiplexer redis,
        ILogger<ExamPaperHelper> logger,
        IRepository<ShuffledExamPaper> shuffledExamPaperRepository,
        IRepository<ExamSessionSubject> examSessionSubjectRepository,
        AutoMapper.IMapper mapper)
    {
        _redis = redis;
        _logger = logger;
        _shuffledExamPaperRepository = shuffledExamPaperRepository;
        _examSessionSubjectRepository = examSessionSubjectRepository;
        _mapper = mapper;
    }

    public async Task<ShuffledExamPaperDto> GetExamFromRedisAsync(int shuffledExamPaperId)
    {
        try
        {
            var db = _redis.GetDatabase();
            string cacheKey = $"shuffled_exam_paper:{shuffledExamPaperId}";
            _logger.LogInformation("Đang tìm đề thi từ Redis với key: {CacheKey}", cacheKey);
            
            var cachedPaper = await db.StringGetAsync(cacheKey);
            
            if (cachedPaper.HasValue)
            {
                var cachedValue = cachedPaper.ToString();
                try 
                {
                    var paperDto = JsonSerializer.Deserialize<ShuffledExamPaperDto>(cachedValue);
                    _logger.LogInformation("Đã lấy được đề thi từ Redis");
                    return paperDto;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi deserialize đề thi từ Redis");
                    await db.KeyDeleteAsync(cacheKey);
                }
            }
            
            // Nếu không có trong Redis, lấy từ database và cache vào Redis
            _logger.LogInformation("Không tìm thấy đề thi trong Redis, đang lấy từ database");
            var (examPaper, examPaperDto) = await GetExamFromDatabaseAsync(shuffledExamPaperId);
            
            if (examPaperDto != null)
            {
                // Cache vào Redis
                await CacheExamPaperAsync(shuffledExamPaperId, examPaperDto, examPaper.AnswerKey);
                _logger.LogInformation("Đã cache đề thi từ database vào Redis");
                
                return examPaperDto;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi truy cập Redis để lấy đề thi");
            return null;
        }
    }

    public async Task<(ShuffledExamPaper ExamPaper, ShuffledExamPaperDto ExamPaperDto)> GetExamFromDatabaseAsync(int shuffledExamPaperId)
    {
        _logger.LogInformation("Lấy đề thi từ database với ID: {ShuffledExamPaperId}", shuffledExamPaperId);
        
        var shuffledExamPaper = await _shuffledExamPaperRepository.GetQueryable()
            .Where(x => x.ShuffledExamPaperId == shuffledExamPaperId)
            .Include(x => x.ShuffledExamPaperDetails)
            .ThenInclude(d => d.OriginalExamPaperDetail)
            .Include(x => x.OriginalExamPaper)
            .Include(x => x.Subject)
            .FirstOrDefaultAsync();
        
        if (shuffledExamPaper == null)
        {
            _logger.LogError("Không tìm thấy đề thi hoán vị {ShuffledExamPaperId} trong database", 
                shuffledExamPaperId);
            throw new Exception("Không tìm thấy đề thi hoán vị.");
        }
        
        var paperDto = _mapper.Map<ShuffledExamPaperDto>(shuffledExamPaper);
        return (shuffledExamPaper, paperDto);
    }

    public async Task<bool> CacheExamPaperAsync(int shuffledExamPaperId, ShuffledExamPaperDto paperDto, string answerKey)
    {
        try
        {
            var db = _redis.GetDatabase();
            string cacheKey = $"shuffled_exam_paper:{shuffledExamPaperId}";
            string answerKeyCache = $"answer_key:{shuffledExamPaperId}";
            
            var jsonString = JsonSerializer.Serialize(paperDto);
            var batch = db.CreateBatch();
            
            // Thực hiện cache đồng thời
            var cacheTask = batch.StringSetAsync(cacheKey, jsonString, TimeSpan.FromHours(6));
            var answerKeyTask = batch.StringSetAsync(answerKeyCache, answerKey, TimeSpan.FromHours(6));
            
            batch.Execute();
            await Task.WhenAll(cacheTask, answerKeyTask);
            
            _logger.LogInformation("Đã cache đề thi và answer key vào Redis");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cache đề thi vào Redis");
            return false;
        }
    }

    public async Task<ShuffledExamPaper> GetRandomExamPaperAsync(int examSessionSubjectId)
    {
        _logger.LogInformation("Sinh viên chưa được gán đề thi, đang chọn đề ngẫu nhiên");
        
        // Lấy ExamSessionSubject và kiểm tra đề gốc
        var examSessionSubject = await _examSessionSubjectRepository.GetQueryable()
            .FirstOrDefaultAsync(x => x.ExamSessionSubjectId == examSessionSubjectId);
        if (examSessionSubject == null)
        {
            _logger.LogError("Không tìm thấy ca thi môn {ExamSessionSubjectId}", examSessionSubjectId);
            throw new Exception("Không tìm thấy ca thi môn này.");
        }
        if (examSessionSubject.OriginalExamPaperId == null)
        {
            _logger.LogError("Ca thi môn {ExamSessionSubjectId} chưa có đề thi gốc", examSessionSubjectId);
            throw new Exception("Chưa có đề thi gốc cho ca thi này.");
        }
        
        // Lấy danh sách đề hoán vị từ repository
        var availablePapers = await _shuffledExamPaperRepository.GetQueryable()
            .Where(p => p.OriginalExamPaperId == examSessionSubject.OriginalExamPaperId && p.IsApproved == true)
            .ToListAsync();
        
        if (!availablePapers.Any())
        {
            _logger.LogError("Không có đề thi hoán vị nào được phê duyệt cho ca thi {ExamSessionSubjectId}", 
                examSessionSubjectId);
            throw new Exception("Chưa có đề thi hoán vị đã được phê duyệt cho ca thi này.");
        }
        
        _logger.LogInformation("Tìm thấy {Count} đề thi hoán vị khả dụng", availablePapers.Count);
        
        var random = new Random();
        var shuffledExamPaper = availablePapers[random.Next(availablePapers.Count)];
        
        _logger.LogInformation("Đã chọn ngẫu nhiên đề thi {ShuffledExamPaperId}", shuffledExamPaper.ShuffledExamPaperId);
        
        return shuffledExamPaper;
    }

    public async Task<(bool Success, string Message, Dictionary<int, string>? CorrectAnswers)> GetAnswerKeyAsync(int shuffledExamPaperId)
    {
        var db = _redis.GetDatabase();
        
        // Lấy answer key từ Redis
        string answerKey = $"answer_key:{shuffledExamPaperId}";
        var answerKeyValue = await db.StringGetAsync(answerKey);
        
        if (!answerKeyValue.HasValue)
        {
            _logger.LogError("Không tìm thấy đáp án trong Redis");
            return (false, "Không tìm thấy đáp án", null);
        }

        string correctAnswers = answerKeyValue.ToString();
        var correctAnswerPairs = correctAnswers.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim('(', ')').Split(','))
            .ToDictionary(parts => int.Parse(parts[0]), parts => parts[1]);

        return (true, "Lấy đáp án thành công", correctAnswerPairs);
    }

    public Dictionary<int, string> ParseAnswerKey(string answerKeyString)
    {
        return answerKeyString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim('(', ')').Split(','))
            .ToDictionary(parts => int.Parse(parts[0]), parts => parts[1]);
    }
} 