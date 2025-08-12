using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Messages;
using backend_manage.core.Messages.RabbitMQ;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using backend_manage.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace backend_manage.core.Services.AuthService.Helpers;

public class ExamPaperHelper
{
    private readonly IRedisService _redisService;
    private readonly ILogger<ExamPaperHelper> _logger;
    private readonly IRepository<ShuffledExamPaper> _shuffledExamPaperRepository;
    private readonly IRepository<ExamSessionSubject> _examSessionSubjectRepository;
    private readonly AutoMapper.IMapper _mapper;
    private readonly StudentExamSessionCacheHelper _sessionCacheHelper;
    private readonly IRabbitMqService _rabbitMqService;

    public ExamPaperHelper(
        IRedisService redisService,
        ILogger<ExamPaperHelper> logger,
        IRepository<ShuffledExamPaper> shuffledExamPaperRepository,
        IRepository<ExamSessionSubject> examSessionSubjectRepository,
        AutoMapper.IMapper mapper,
        StudentExamSessionCacheHelper sessionCacheHelper,
        IRabbitMqService rabbitMqService
        )
    {
        _redisService = redisService;
        _logger = logger;
        _shuffledExamPaperRepository = shuffledExamPaperRepository;
        _examSessionSubjectRepository = examSessionSubjectRepository;
        _mapper = mapper;
        _sessionCacheHelper = sessionCacheHelper;
        _rabbitMqService = rabbitMqService;
    }

    //dùng để bắt đầu thi()
    #region GetStudentExamSessionAndExamPaperAsync
    public async Task<(StudentExamSessionCacheDto studentExamSessionDto, ShuffledExamPaperDto? existingExamPaper)> GetStudentExamSessionAndExamPaperAsync(string studentCode, int studentExamSessionId)
    {
        var (redisAvailable, studentExamSessionDto) = await _sessionCacheHelper.GetStudentExamSessionAsync(studentCode, studentExamSessionId);

        if (studentExamSessionDto == null)
        {
            _logger.LogWarning("Không tìm thấy phiên thi của sinh viên {StudentCode} với ID phiên thi {SessionId}", studentCode, studentExamSessionId);
            return (null, null);
        }

        // Kiểm tra thời gian bắt đầu thi
        var currentTime = DateTimeHelper.GetVietnamTime();
        var examStartTime = studentExamSessionDto.ExamSessionStartTime;
        var timeDifference = currentTime - examStartTime;

        // Không được thi sớm hơn thời gian bắt đầu
        if (currentTime < examStartTime)
        {
            var minutesEarly = Math.Abs(timeDifference.TotalMinutes);
            _logger.LogWarning("Sinh viên {StudentCode} cố gắng thi sớm {Minutes} phút. Thời gian bắt đầu: {StartTime}, Thời gian hiện tại: {CurrentTime}", 
                studentCode, minutesEarly, examStartTime, currentTime);
            throw new InvalidOperationException($"Chưa đến thời gian làm bài. Ca thi bắt đầu lúc {examStartTime:HH:mm dd/MM/yyyy}");
        }

        // Kiểm tra logic mới: nếu sinh viên đã bắt đầu làm bài (StartTime != null) và phiên thi vẫn còn hiệu lực
        if (studentExamSessionDto.StartTime.HasValue)
        {
            // Tính thời gian kết thúc dự kiến dựa trên thời gian bắt đầu làm bài của sinh viên
            var studentStartTime = studentExamSessionDto.StartTime.Value;
            var totalDuration = studentExamSessionDto.Duration + studentExamSessionDto.ExtraMinutes;
            var expectedEndTime = examStartTime.AddMinutes(totalDuration);

            // Kiểm tra xem phiên thi có còn hiệu lực không
            if (currentTime <= expectedEndTime)
            {
                _logger.LogInformation("Sinh viên {StudentCode} đã bắt đầu làm bài từ {StudentStartTime} và vẫn còn trong thời gian hiệu lực. Thời gian hiện tại: {CurrentTime}, Dự kiến kết thúc: {ExpectedEndTime}", 
                    studentCode, studentStartTime, currentTime, expectedEndTime);
            }
            else
            {
                _logger.LogWarning("Sinh viên {StudentCode} đã hết thời gian làm bài. Bắt đầu: {StudentStartTime}, Kết thúc dự kiến: {ExpectedEndTime}, Thời gian hiện tại: {CurrentTime}", 
                    studentCode, studentStartTime, expectedEndTime, currentTime);
                throw new InvalidOperationException($"Đã hết thời gian làm bài. Bạn đã bắt đầu làm bài lúc {studentStartTime:HH:mm dd/MM/yyyy} và thời gian làm bài đã kết thúc lúc {expectedEndTime:HH:mm dd/MM/yyyy}");
            }
        }
        else
        {
            // Logic cũ: chỉ áp dụng cho sinh viên chưa bắt đầu làm bài (StartTime == null)
            // Không được thi muộn quá 15 phút khi lần đầu tiên bắt đầu
            if (timeDifference.TotalMinutes > 15)
            {
                _logger.LogWarning("Sinh viên {StudentCode} cố gắng thi muộn {Minutes} phút. Thời gian bắt đầu: {StartTime}, Thời gian hiện tại: {CurrentTime}", 
                    studentCode, timeDifference.TotalMinutes, examStartTime, currentTime);
                throw new InvalidOperationException($"Đã quá thời gian cho phép bắt đầu làm bài. Ca thi bắt đầu lúc {examStartTime:HH:mm dd/MM/yyyy}, chỉ được muộn tối đa 15 phút");
            }
        }

        _logger.LogInformation("Sinh viên {StudentCode} bắt đầu thi đúng thời gian. Thời gian bắt đầu: {StartTime}, Thời gian hiện tại: {CurrentTime}, Chênh lệch: {Minutes} phút", 
            studentCode, examStartTime, currentTime, timeDifference.TotalMinutes);
        
        if (!studentExamSessionDto.ShuffledExamPaperId.HasValue)
        {
            _logger.LogInformation("Chưa có đề thi nên sẽ random đề thi mới cho sinh viên {StudentCode}", studentCode);
            var newExamPaper = await CreateNewExamPaperAsync(studentExamSessionDto.ExamSessionSubjectId, studentCode);
            _logger.LogInformation("Cập nhật đề thi vào phiên thi trên redis và db");
            studentExamSessionDto.ShuffledExamPaperId = newExamPaper.ShuffledExamPaperId;
            studentExamSessionDto.StartTime = DateTimeHelper.GetVietnamTime();
            
            // Tạo chuỗi đáp án rỗng dựa trên cấu trúc đề thi thực tế
            var emptyAnswers = CreateEmptyAnswersString(newExamPaper.AnswerKey);
            studentExamSessionDto.StudentAnswersString = emptyAnswers;
            studentExamSessionDto.IsCompleted = false; // Chưa hoàn thành
            await _sessionCacheHelper.UpdateStudentExamSessionAsync(studentCode,studentExamSessionDto);

            var startExamMessage = new StartExamMessage
            {
                StudentExamSessionId = studentExamSessionId,
                StudentCode = studentCode,
                StartTime = DateTimeHelper.GetVietnamTime(),
                ShuffledExamPaperId = newExamPaper.ShuffledExamPaperId,
                StudentAnswersString = emptyAnswers,
                IsCompleted = false
            };
            
            _rabbitMqService.Publish("start_exam_queue",startExamMessage);
            _logger.LogInformation("Đã gửi đến message để lưu thông tin vào db cho sinh viên {studentCode}",studentCode);
            
            return (studentExamSessionDto, newExamPaper);
        }

        _logger.LogInformation("Đã có đề thi, tiến hành lấy từ Redis với ID {ShuffledExamPaperId}", studentExamSessionDto.ShuffledExamPaperId.Value);
        var (success, existingExamPaper) = await GetExamFromRedisAsync(studentExamSessionDto.ShuffledExamPaperId.Value);
        
        // Nếu không lấy được từ Redis, thử lấy từ database
        if (!success || existingExamPaper == null)
        {
            _logger.LogInformation("Không lấy được đề thi từ Redis, chuyển sang lấy từ database");
            existingExamPaper = await GetExamFromDatabaseAsync(studentExamSessionDto.ShuffledExamPaperId.Value);
        }
        
        return (studentExamSessionDto, existingExamPaper);
    }
    
    
    //chọn đề thi cho sinh viên
    private async Task<ShuffledExamPaperDto> CreateNewExamPaperAsync(int examSessionSubjectId, string studentCode)
    {
        var shuffledExamPaper = await GetRandomExamPaperAsync(examSessionSubjectId);
        
        _logger.LogInformation("Đã chọn ngẫu nhiên đề thi {ShuffledExamPaperId} cho sinh viên {StudentCode}", shuffledExamPaper.ShuffledExamPaperId, studentCode);
        
        // Thử lấy đề từ cache trước, fallback về database
        var paperDto = await TryGetExamFromCacheAsync(shuffledExamPaper.ShuffledExamPaperId, studentCode);
        if (paperDto == null)
        {
            _logger.LogInformation("Đang lấy đề thi từ db");
            var pp  = await GetExamFromDatabaseAsync(shuffledExamPaper.ShuffledExamPaperId);
            paperDto = pp;
        }
        
        return paperDto;
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
                .Include(x => x.ShuffledExamPaperDetails)
                    .ThenInclude(d => d.OriginalExamPaperDetail)
                .Include(x => x.ShuffledExamPaperDetails)
                    .ThenInclude(d => d.ChildQuestions)
                .Include(x => x.ShuffledExamPaperDetails)
                    .ThenInclude(d => d.ParentQuestion)
                .Include(x => x.OriginalExamPaper)
                .Include(x => x.Subject)
                .FirstOrDefaultAsync(p => p.ShuffledExamPaperId == randomPaperId.Value);
            
            if (cachedExamPaper != null)
            {
                _logger.LogInformation("Đã chọn ngẫu nhiên đề thi {ShuffledExamPaperId} từ cache", cachedExamPaper.ShuffledExamPaperId);
                return cachedExamPaper;
            }
        }
        
        // Fallback về database nếu cache miss
        _logger.LogInformation("Không lấy được danh sách đề trên redis,chuyển sang lấy từ database");
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

    
    #endregion
    
    

    private async Task<ShuffledExamPaperDto?> TryGetExamFromCacheAsync(int shuffledExamPaperId, string studentCode)
    {
        try
        {
            _logger.LogInformation("Thử lấy đề thi {ShuffledExamPaperId} từ Redis", shuffledExamPaperId);
            var (s,paperDto) = await GetExamFromRedisAsync(shuffledExamPaperId);
            if (paperDto != null)
            {
                _logger.LogInformation("Đã lấy được đề thi từ Redis cho sinh viên {StudentCode}", studentCode);
                return paperDto;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi lấy đề thi từ Redis, sẽ lấy từ database");
        }
        return null;
    }

   
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
            
            _logger.LogInformation("Đã cache {Count} đề thi cho OriginalExamPaperId cho đề gốc  {OriginalExamPaperId}", 
                papers.Count, originalExamPaperId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi cache available papers cho OriginalExamPaperId {OriginalExamPaperId}", 
                originalExamPaperId);
        }
    }
    
    
    public async Task<(bool success, ShuffledExamPaperDto? examPaper)> GetExamFromRedisAsync(int shuffledExamPaperId)
    {
        try
        {
            var db = _redisService.GetDatabase();
            string cacheKey = $"shuffled_exam_paper:{shuffledExamPaperId}";
            _logger.LogInformation("Đang tìm đề thi từ Redis với key: {CacheKey}", cacheKey);

            var cachedPaper = await db.StringGetAsync(cacheKey);

            if (cachedPaper.HasValue)
            {
                var cachedValue = cachedPaper.ToString();
                try
                {
                    var jsonOptions = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.Preserve,
                    MaxDepth = 64
                };
                var paperDto = JsonSerializer.Deserialize<ShuffledExamPaperDto>(cachedValue, jsonOptions);
                    if (paperDto != null)
                    {
                        _logger.LogInformation("Đã lấy được đề thi từ Redis");
                        return (true, paperDto);
                    }
                    else
                    {
                        _logger.LogWarning("Dữ liệu Redis null sau khi deserialize, xóa key: {CacheKey}", cacheKey);
                        await db.KeyDeleteAsync(cacheKey);
                        return (false, null);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi deserialize đề thi từ Redis, xóa key: {CacheKey}", cacheKey);
                    await db.KeyDeleteAsync(cacheKey);
                    return (false, null);
                }
            }
            else
            {
                _logger.LogInformation("Không tìm thấy đề thi trong Redis với key: {CacheKey}", cacheKey);
                return (false, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi truy cập Redis để lấy đề thi");
            return (false, null);
        }
    }

    #region GetExamFromDatabaseAsync
    private async Task<ShuffledExamPaperDto> GetExamFromDatabaseAsync(int shuffledExamPaperId)
    {
        _logger.LogInformation("Lấy đề thi từ database cho ShuffledExamPaperId: {ShuffledExamPaperId}", shuffledExamPaperId);
        var (examPaper, examPaperDto) = await GetExamFromDatabase(shuffledExamPaperId);
        
        // Cache lại vào Redis
        await CacheExamPaperAsync(shuffledExamPaperId, examPaperDto);
        
        return examPaperDto;
    }
    
    public async Task<(ShuffledExamPaper ExamPaper, ShuffledExamPaperDto ExamPaperDto)> GetExamFromDatabase(int shuffledExamPaperId)
    {
        var shuffledExamPaper = await _shuffledExamPaperRepository.GetQueryable()
            .Include(x => x.ShuffledExamPaperDetails)
                .ThenInclude(d => d.OriginalExamPaperDetail)
            .Include(x => x.ShuffledExamPaperDetails)
                .ThenInclude(d => d.ChildQuestions)
            .Include(x => x.ShuffledExamPaperDetails)
                .ThenInclude(d => d.ParentQuestion)
            .Include(x => x.OriginalExamPaper)
            .Include(x => x.Subject)
            .FirstOrDefaultAsync(x => x.ShuffledExamPaperId == shuffledExamPaperId);

        if (shuffledExamPaper == null)
        {
            _logger.LogError("Không tìm thấy đề thi hoán vị với ID: {ShuffledExamPaperId}", shuffledExamPaperId);
            throw new Exception("Không tìm thấy đề thi hoán vị.");
        }

        var paperDto = _mapper.Map<ShuffledExamPaperDto>(shuffledExamPaper);
        return (shuffledExamPaper, paperDto);
    }


    public async Task<bool> CacheExamPaperAsync(int shuffledExamPaperId, ShuffledExamPaperDto paperDto)
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
            
            var jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve,
                MaxDepth = 64
            };
            var jsonString = JsonSerializer.Serialize(paperDto, jsonOptions);
            await db.StringSetAsync(cacheKey, jsonString, TimeSpan.FromHours(6));
            
            _logger.LogInformation("Đã cache đề thi vào Redis ");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cache đề thi vào Redis");
            return false;
        }
    }
    #endregion
    // /// // // // // //
   
    public Dictionary<string, string> ParseAnswerKey(string answerKeyString)
    {
        return answerKeyString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim('(', ')').Split(','))
            .ToDictionary(parts => parts[0], parts => parts[1]);
    }
    
    private string CreateEmptyAnswersString(string answerKey)
    {
        if (string.IsNullOrWhiteSpace(answerKey))
            return "";

        // Tạo chuỗi đáp án rỗng bằng cách thay thế tất cả các ký tự A, B, C, D bằng dấu '-'
        // Giữ nguyên cấu trúc format của answer key
        string result = answerKey;
        
        // Thay thế tất cả các ký tự A, B, C, D bằng dấu '-'
        result = Regex.Replace(result, @"[A-D]", "-");
        
        return result;
    }


    public async Task<(bool s, double core, string message, StudentExamSessionCacheDto? dto, string? answerKey)> SubmitExam(string studentCode, int studentExamSessionId)
    {
        try
        {
            _logger.LogInformation("🔄 Bắt đầu xử lý nộp bài thi cho sinh viên {StudentCode} với session {SessionId}", studentCode, studentExamSessionId);
            
            var (redisAvailable, studentExamSessionDto) = await _sessionCacheHelper.GetStudentExamSessionAsync(studentCode, studentExamSessionId);

            if (studentExamSessionDto == null)
            {
                _logger.LogWarning("❌ Không tìm thấy phiên thi của sinh viên {StudentCode} với ID phiên thi {SessionId}", studentCode, studentExamSessionId);
                return (false, 0, "Không tìm thấy phiên thi", null, null);
            }

            if (studentExamSessionDto.IsCompleted)
            {
                _logger.LogWarning("⚠️ Sinh viên {StudentCode} đã nộp bài thi trước đó", studentCode);
                return (false, 0, "Đã nộp bài thi trước đó", null, null);
            }

            if (!studentExamSessionDto.ShuffledExamPaperId.HasValue)
            {
                _logger.LogWarning("❌ Sinh viên {StudentCode} chưa có đề thi được gán", studentCode);
                return (false, 0, "Chưa có đề thi được gán", null, null);
            }

            // Lấy đề thi từ Redis, nếu không có thì lấy từ database
            var (s,paperDto) = await GetExamFromRedisAsync(studentExamSessionDto.ShuffledExamPaperId.Value);
            if (paperDto == null)
            {
                _logger.LogWarning("⚠️ Không thể lấy đề thi từ Redis cho ShuffledExamPaperId {PaperId}, thử lấy từ database", studentExamSessionDto.ShuffledExamPaperId.Value);
                try
                {
                    paperDto = await GetExamFromDatabaseAsync(studentExamSessionDto.ShuffledExamPaperId.Value);
                    if (paperDto == null)
                    {
                        _logger.LogError("❌ Không thể lấy đề thi từ database cho ShuffledExamPaperId {PaperId}", studentExamSessionDto.ShuffledExamPaperId.Value);
                        return (false, 0, "Không thể lấy đề thi", null, null);
                    }
                    _logger.LogInformation("✅ Đã lấy được đề thi từ database cho ShuffledExamPaperId {PaperId}", studentExamSessionDto.ShuffledExamPaperId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Lỗi khi lấy đề thi từ database cho ShuffledExamPaperId {PaperId}", studentExamSessionDto.ShuffledExamPaperId.Value);
                    return (false, 0, "Không thể lấy đề thi", null, null);
                }
            }

            
            
            if (string.IsNullOrEmpty(paperDto.AnswerKey))
            {
                _logger.LogError("❌ Đề thi không có đáp án chuẩn");
                return (false, 0, "Đề thi không có đáp án chuẩn", null, null);
            }

            // Tính điểm và đếm câu đúng
            var (score, correctAnswers, totalQuestions) = CalculateScore(
                studentExamSessionDto.StudentAnswersString, 
                paperDto.AnswerKey
            );

            // Cập nhật thông tin phiên thi
            var endTime = DateTimeHelper.GetVietnamTime();
            studentExamSessionDto.EndTime = endTime;
            studentExamSessionDto.Score = score;
            studentExamSessionDto.CorrectAnswers = correctAnswers;
            studentExamSessionDto.TotalQuestions = totalQuestions;
            studentExamSessionDto.IsCompleted = true;

            // Cập nhật vào Redis
            await _sessionCacheHelper.UpdateStudentExamSessionAsync(studentCode, studentExamSessionDto);

            // Tạo message để lưu vào database
            var examSubmissionMessage = new ExamSubmissionMessage
            {
                StudentCode = studentCode,
                ShuffledExamPaperId = studentExamSessionDto.ShuffledExamPaperId.Value,
                Score = score,
                CorrectAnswers = correctAnswers,
                TotalQuestions = totalQuestions,
                IsCompleted = true,
                EndTime = endTime,
                StudentAnswersString = studentExamSessionDto.StudentAnswersString
            };

            // Gửi message qua RabbitMQ
            _rabbitMqService.Publish("exam_submission_queue", examSubmissionMessage);
            _logger.LogInformation("📤 Đã gửi message nộp bài thi qua RabbitMQ cho sinh viên {StudentCode}", studentCode);

            _logger.LogInformation("✅ Hoàn thành nộp bài thi cho sinh viên {StudentCode}. Điểm: {Score}, Đúng: {CorrectAnswers}/{TotalQuestions}", 
                studentCode, score, correctAnswers, totalQuestions);

            return (true, score, $"Nộp bài thi thành công. Điểm: {score:F2}, Đúng: {correctAnswers}/{totalQuestions} câu", studentExamSessionDto, paperDto.AnswerKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi nộp bài thi cho sinh viên {StudentCode}", studentCode);
            return (false, 0, $"Lỗi hệ thống: {ex.Message}", null, null);
        }
    }

    private (double score, int correctAnswers, int totalQuestions) CalculateScore(string studentAnswers, string answerKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(studentAnswers) || string.IsNullOrWhiteSpace(answerKey))
            {
                _logger.LogWarning("⚠️ Chuỗi đáp án sinh viên hoặc đáp án chuẩn rỗng");
                return (0, 0, 0);
            }

            var studentAnswerPairs = ParseAnswerKey(studentAnswers);     // Dictionary<string, string>
            var correctAnswerPairs = ParseAnswerKey(answerKey);          // Dictionary<string, string>

            int correctCount = 0;
            int totalCount = 0;

            foreach (var correctPair in correctAnswerPairs)
            {
                var key = correctPair.Key;
                var correctAnswer = correctPair.Value;

                totalCount++;

                if (studentAnswerPairs.TryGetValue(key, out var studentAnswer))
                {
                    if (!string.IsNullOrWhiteSpace(studentAnswer) && studentAnswer != "-")
                    {
                        if (string.Equals(studentAnswer, correctAnswer, StringComparison.OrdinalIgnoreCase))
                        {
                            correctCount++;
                        }
                    }
                }
            }

            double score = totalCount > 0 ? (double)correctCount / totalCount * 10 : 0;

            _logger.LogInformation("📊 Kết quả tính điểm: Đúng {CorrectCount}/{TotalCount}, Điểm: {Score:F2}",
                correctCount, totalCount, score);

            return (score, correctCount, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi tính điểm");
            return (0, 0, 0);
        }
    }

    
} 