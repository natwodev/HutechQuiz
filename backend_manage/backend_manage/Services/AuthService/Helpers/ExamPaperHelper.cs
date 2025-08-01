using backend_manage.Entities;
using backend_manage.DTOs;
using backend_manage.Repositories;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using backend_manage.Repositories.Interfaces;
using backend_manage.Services.Interfaces;

namespace backend_manage.Services.AuthService.Helpers;

public class ExamPaperHelper
{
    private readonly IRedisService _redisService;
    private readonly ILogger<ExamPaperHelper> _logger;
    private readonly IRepository<ShuffledExamPaper> _shuffledExamPaperRepository;
    private readonly IRepository<ExamSessionSubject> _examSessionSubjectRepository;
    private readonly AutoMapper.IMapper _mapper;

    public ExamPaperHelper(
        IRedisService redisService,
        ILogger<ExamPaperHelper> logger,
        IRepository<ShuffledExamPaper> shuffledExamPaperRepository,
        IRepository<ExamSessionSubject> examSessionSubjectRepository,
        AutoMapper.IMapper mapper)
    {
        _redisService = redisService;
        _logger = logger;
        _shuffledExamPaperRepository = shuffledExamPaperRepository;
        _examSessionSubjectRepository = examSessionSubjectRepository;
        _mapper = mapper;
    }

    // Cache danh sách ShuffledExamPaperId theo OriginalExamPaperId
    private async Task CacheAvailablePapersAsync(int originalExamPaperId, List<ShuffledExamPaper> papers)
    {
        try
        {
            // Kiểm tra Redis connection trước khi cache
            if (!_redisService.IsConnected)
            {
                _logger.LogWarning("Redis không khả dụng, bỏ qua cache available papers cho OriginalExamPaperId {OriginalExamPaperId}", 
                    originalExamPaperId);
                return;
            }

            var db = _redisService.GetDatabase();
            string cacheKey = $"available_papers:{originalExamPaperId}";
            
            // Convert sang array of RedisValue
            var paperIds = papers.Select(p => (RedisValue)p.ShuffledExamPaperId).ToArray();
            
            // Lưu vào Redis Set với TTL 6 giờ
            await db.SetAddAsync(cacheKey, paperIds);
            await db.KeyExpireAsync(cacheKey, TimeSpan.FromHours(6));
            
            _logger.LogInformation("Đã cache {Count} đề thi cho OriginalExamPaperId {OriginalExamPaperId}", 
                papers.Count, originalExamPaperId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi cache available papers cho OriginalExamPaperId {OriginalExamPaperId}", 
                originalExamPaperId);
        }
    }

    // Lấy random ShuffledExamPaperId từ cache
    private async Task<int?> GetRandomPaperIdFromCacheAsync(int originalExamPaperId)
    {
        try
        {
            // Kiểm tra Redis connection trước khi lấy từ cache
            if (!_redisService.IsConnected)
            {
                _logger.LogDebug("Redis không khả dụng, bỏ qua lấy random paper từ cache cho OriginalExamPaperId {OriginalExamPaperId}", 
                    originalExamPaperId);
                return null;
            }

            var db = _redisService.GetDatabase();
            string cacheKey = $"available_papers:{originalExamPaperId}";
            
            // Kiểm tra key có tồn tại không
            if (!await db.KeyExistsAsync(cacheKey))
            {
                _logger.LogDebug("Cache miss cho available_papers:{OriginalExamPaperId}", originalExamPaperId);
                return null;
            }
            
            // Lấy random member từ Set
            var randomId = await db.SetRandomMemberAsync(cacheKey);
            if (randomId.HasValue && int.TryParse(randomId.ToString(), out int paperId))
            {
                _logger.LogDebug("Đã lấy random paper ID {PaperId} từ cache cho OriginalExamPaperId {OriginalExamPaperId}", 
                    paperId, originalExamPaperId);
                return paperId;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi lấy random paper từ cache cho OriginalExamPaperId {OriginalExamPaperId}", 
                originalExamPaperId);
            return null;
        }
    }

    // Preload tất cả approved papers vào cache
    public async Task PreloadAllApprovedPapersAsync()
    {
        try
        {
            // Kiểm tra Redis connection trước khi preload
            if (!_redisService.IsConnected)
            {
                _logger.LogWarning("Redis không khả dụng, bỏ qua preload approved papers vào cache");
                return;
            }

            _logger.LogInformation("Bắt đầu preload tất cả approved papers vào cache");
            
            // Lấy tất cả OriginalExamPaperId có approved papers
            var originalExamPaperIds = await _shuffledExamPaperRepository.GetQueryable()
                .Where(p => p.IsApproved == true)
                .Select(p => p.OriginalExamPaperId)
                .Distinct()
                .ToListAsync();
            
            foreach (var originalExamPaperId in originalExamPaperIds)
            {
                var papers = await _shuffledExamPaperRepository.GetQueryable()
                    .Where(p => p.OriginalExamPaperId == originalExamPaperId && p.IsApproved == true)
                    .ToListAsync();
                
                await CacheAvailablePapersAsync(originalExamPaperId, papers);
            }
            
            _logger.LogInformation("Hoàn thành preload {Count} OriginalExamPaper vào cache", originalExamPaperIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi preload approved papers vào cache");
        }
    }

    public async Task<ShuffledExamPaperDto> GetExamFromRedisAsync(int shuffledExamPaperId)
    {
        try
        {
            // Kiểm tra Redis connection trước khi lấy từ cache
            if (!_redisService.IsConnected)
            {
                _logger.LogDebug("Redis không khả dụng, bỏ qua lấy đề thi từ cache cho ShuffledExamPaperId {ShuffledExamPaperId}", 
                    shuffledExamPaperId);
                return null;
            }

            var db = _redisService.GetDatabase();
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
            // Kiểm tra Redis connection trước khi cache
            if (!_redisService.IsConnected)
            {
                _logger.LogWarning("Redis không khả dụng, bỏ qua cache đề thi vào Redis cho ShuffledExamPaperId {ShuffledExamPaperId}", 
                    shuffledExamPaperId);
                return false;
            }

            var db = _redisService.GetDatabase();
            string cacheKey = $"shuffled_exam_paper:{shuffledExamPaperId}";
            
            var jsonString = JsonSerializer.Serialize(paperDto);
            await db.StringSetAsync(cacheKey, jsonString, TimeSpan.FromHours(6));
            
            _logger.LogInformation("Đã cache đề thi vào Redis (AnswerKey đã được include trong paperDto)");
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
        
        // Thử lấy từ cache trước
        var randomPaperId = await GetRandomPaperIdFromCacheAsync(examSessionSubject.OriginalExamPaperId.Value);
        if (randomPaperId.HasValue)
        {
            // Lấy chi tiết đề từ database
            var cachedExamPaper = await _shuffledExamPaperRepository.GetQueryable()
                .FirstOrDefaultAsync(p => p.ShuffledExamPaperId == randomPaperId.Value);
            
            if (cachedExamPaper != null)
            {
                _logger.LogInformation("Đã chọn ngẫu nhiên đề thi {ShuffledExamPaperId} từ cache", cachedExamPaper.ShuffledExamPaperId);
                return cachedExamPaper;
            }
        }
        
        // Fallback về database nếu cache miss
        _logger.LogInformation("Cache miss, lấy từ database");
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
        
        // Cache lại cho lần sau
        await CacheAvailablePapersAsync(examSessionSubject.OriginalExamPaperId.Value, availablePapers);
        
        var random = new Random();
        var selectedExamPaper = availablePapers[random.Next(availablePapers.Count)];
        
        _logger.LogInformation("Đã chọn ngẫu nhiên đề thi {ShuffledExamPaperId}", selectedExamPaper.ShuffledExamPaperId);
        return selectedExamPaper;
    }

    public async Task<(bool Success, string Message, Dictionary<int, string>? CorrectAnswers)> GetAnswerKeyAsync(int shuffledExamPaperId)
    {
        try
        {
            // Lấy đề thi từ cache (đã include AnswerKey)
            var examPaperDto = await GetExamFromRedisAsync(shuffledExamPaperId);
            
            if (examPaperDto == null)
            {
                _logger.LogError("Không tìm thấy đề thi trong Redis");
                return (false, "Không tìm thấy đề thi", null);
            }

            if (string.IsNullOrEmpty(examPaperDto.AnswerKey))
            {
                _logger.LogError("Đề thi không có AnswerKey");
                return (false, "Đề thi không có AnswerKey", null);
            }

            string correctAnswers = examPaperDto.AnswerKey;
            var correctAnswerPairs = correctAnswers.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim('(', ')').Split(','))
                .ToDictionary(parts => int.Parse(parts[0]), parts => parts[1]);

            return (true, "Lấy đáp án thành công", correctAnswerPairs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy AnswerKey từ cache");
            return (false, "Lỗi khi lấy AnswerKey: " + ex.Message, null);
        }
    }

    public Dictionary<int, string> ParseAnswerKey(string answerKeyString)
    {
        return answerKeyString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim('(', ')').Split(','))
            .ToDictionary(parts => int.Parse(parts[0]), parts => parts[1]);
    }
} 