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
    private readonly IRepository<OriginalExamPaper> _originalExamPaperRepository;
    private readonly AutoMapper.IMapper _mapper;
    private readonly StudentExamSessionCacheHelper _sessionCacheHelper;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly IMessageProcessingService _messageProcessingService;

    public ExamPaperHelper(
        IRedisService redisService,
        ILogger<ExamPaperHelper> logger,
        IRepository<ShuffledExamPaper> shuffledExamPaperRepository,
        IRepository<ExamSessionSubject> examSessionSubjectRepository,
        IRepository<OriginalExamPaper> originalExamPaperRepository,
        AutoMapper.IMapper mapper,
        StudentExamSessionCacheHelper sessionCacheHelper,
        IRabbitMqService rabbitMqService,
        IMessageProcessingService messageProcessingService
        )
    {
        _redisService = redisService;
        _logger = logger;
        _shuffledExamPaperRepository = shuffledExamPaperRepository;
        _examSessionSubjectRepository = examSessionSubjectRepository;
        _originalExamPaperRepository = originalExamPaperRepository;
        _mapper = mapper;
        _sessionCacheHelper = sessionCacheHelper;
        _rabbitMqService = rabbitMqService;
        _messageProcessingService = messageProcessingService;
    }

    //dùng để bắt đầu thi()
    #region GetStudentExamSessionAndExamPaperAsync
    
    #region GetStudentExamSessionAndExamPaperAsync
    public async Task<(StudentExamSessionCacheDto studentExamSessionDto, ShuffledExamPaperDto? existingExamPaper, OriginalExamPaperDto? originalExamPaper)> GetStudentExamSessionAndExamPaperAsync(string studentCode, int studentExamSessionId)
    {
        var (redisAvailable, studentExamSessionDto) = await _sessionCacheHelper.GetStudentExamSessionAsync(studentCode, studentExamSessionId);

        if (studentExamSessionDto == null)
        {
            _logger.LogWarning("Không tìm thấy phiên thi của sinh viên {StudentCode} với ID phiên thi {SessionId}", studentCode, studentExamSessionId);
            return (null, null, null);
        }

        // Kiểm tra trạng thái hoạt động của môn thi trong ca thi
        var examSessionSubject = await _examSessionSubjectRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ExamSessionSubjectId == studentExamSessionDto.ExamSessionSubjectId);
        if (examSessionSubject == null)
        {
            _logger.LogWarning("Không tìm thấy ExamSessionSubject với ID {ExamSessionSubjectId} cho sinh viên {StudentCode}", studentExamSessionDto.ExamSessionSubjectId, studentCode);
            throw new InvalidOperationException("Không tìm thấy thông tin ca thi môn.");
        }
        if (!examSessionSubject.IsActive)
        {
            _logger.LogWarning("ExamSessionSubject {ExamSessionSubjectId} chưa được mở (IsActive = false) cho sinh viên {StudentCode}", examSessionSubject.ExamSessionSubjectId, studentCode);
            throw new InvalidOperationException("Môn thi chưa được mở. Vui lòng liên hệ giám thị.");
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

        bool isNewStartTime = false;
        if (!studentExamSessionDto.StartTime.HasValue)
        {
            studentExamSessionDto.StartTime = DateTimeHelper.GetVietnamTime();
            isNewStartTime = true;
        }

        if (!studentExamSessionDto.ShuffledExamPaperId.HasValue)
        {
            _logger.LogInformation("Chưa có đề thi nên sẽ random đề thi mới cho sinh viên {StudentCode}", studentCode);
            var newExamPaper = await CreateNewExamPaperAsync(studentExamSessionDto.ExamSessionSubjectId, studentCode);
            _logger.LogInformation("Cập nhật đề thi vào phiên thi trên redis và db");
            
            studentExamSessionDto.ShuffledExamPaperId = newExamPaper.ShuffledExamPaperId;
            studentExamSessionDto.OriginalExamPaperId = newExamPaper.OriginalExamPaperId;
            
            // Tính toán thời gian còn lại khi bắt đầu làm bài
            var originalExamStartTime = studentExamSessionDto.ExamSessionStartTime; // A
            var studentStartTime = studentExamSessionDto.StartTime.Value; // B
            var initialMinutesPassed = (int)(studentStartTime - originalExamStartTime).TotalMinutes; // C
            studentExamSessionDto.RemainingMinutes = (studentExamSessionDto.Duration + studentExamSessionDto.ExtraMinutes) - initialMinutesPassed; // D
            
            var originalExamPaperDto = await GetOriginalExamPaperAsync(newExamPaper.OriginalExamPaperId);

            // Tạo chuỗi đáp án rỗng dựa trên cấu trúc đề thi thực tế
            var emptyAnswers = CreateEmptyAnswersString(originalExamPaperDto.KeyValueList);
            studentExamSessionDto.StudentAnswersString = emptyAnswers;
            studentExamSessionDto.IsCompleted = false; 

            await _sessionCacheHelper.UpdateStudentExamSessionAsync(studentCode, studentExamSessionDto);

            var startExamMessage = new StartExamMessage
            {
                StudentExamSessionId = studentExamSessionId,
                StudentCode = studentCode,
                StartTime = studentExamSessionDto.StartTime.Value,
                ShuffledExamPaperId = newExamPaper.ShuffledExamPaperId,
                OriginalExamPaperId = newExamPaper.OriginalExamPaperId,
                StudentAnswersString = emptyAnswers,
                IsCompleted = false,
                RemainingMinutes = studentExamSessionDto.RemainingMinutes
            };
            
            try
            {
                _rabbitMqService.Publish("start_exam_queue", startExamMessage);
                _logger.LogInformation("Đã gửi đến message để lưu thông tin vào db cho sinh viên {studentCode}", studentCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ publish thất bại, fallback xử lý trực tiếp start_exam_queue cho {studentCode}", studentCode);
                await _messageProcessingService.ProcessMessageAsync("start_exam_queue", startExamMessage);
            }
            
            return (studentExamSessionDto, newExamPaper, originalExamPaperDto);
        }

        // Trường hợp đã có đề thi nhưng có thể chưa có StartTime (do gán đề trước)
        if (isNewStartTime)
        {
            await _sessionCacheHelper.UpdateStudentExamSessionAsync(studentCode, studentExamSessionDto);
            
            var startExamMessage = new StartExamMessage
            {
                StudentExamSessionId = studentExamSessionId,
                StudentCode = studentCode,
                StartTime = studentExamSessionDto.StartTime.Value,
                ShuffledExamPaperId = studentExamSessionDto.ShuffledExamPaperId ?? 0,
                OriginalExamPaperId = studentExamSessionDto.OriginalExamPaperId ?? 0,
                StudentAnswersString = studentExamSessionDto.StudentAnswersString ?? "",
                IsCompleted = false,
                RemainingMinutes = studentExamSessionDto.RemainingMinutes
            };

            try
            {
                _rabbitMqService.Publish("start_exam_queue", startExamMessage);
                _logger.LogInformation("Đã gửi start_exam_message cho trường hợp đã có đề nhưng mới bắt đầu thi: {studentCode}", studentCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ publish thất bại cho {studentCode}", studentCode);
            }
        }

        _logger.LogInformation("Đã có đề thi, tiến hành lấy từ Redis với ID {ShuffledExamPaperId}", studentExamSessionDto.ShuffledExamPaperId.Value);
        
        // Cập nhật RemainingMinutes mỗi lần sinh viên nhấn "Bắt đầu thi"
        // A: StartTime (thời gian bắt đầu gốc) = studentExamSessionDto.ExamSessionStartTime
        // B: Thời gian sinh viên vào làm bài hiện tại = DateTimeHelper.GetVietnamTime()
        // C: Số phút đã qua = B - A
        // D: RemainingMinutes = (Duration + ExtraMinutes) - C
        var currentMinutesPassed = (int)(DateTimeHelper.GetVietnamTime() - studentExamSessionDto.ExamSessionStartTime).TotalMinutes; // C
        studentExamSessionDto.RemainingMinutes = (studentExamSessionDto.Duration + studentExamSessionDto.ExtraMinutes) - currentMinutesPassed; // D
        
        // Cập nhật Redis cache với RemainingMinutes mới
        await _sessionCacheHelper.UpdateStudentExamSessionAsync(studentCode, studentExamSessionDto);
        
        var (success, existingExamPaper) = await GetExamFromRedisAsync(studentExamSessionDto.ShuffledExamPaperId.Value);
        
        // Nếu không lấy được từ Redis, thử lấy từ database
        if (!success || existingExamPaper == null)
        {
            _logger.LogInformation("Không lấy được đề thi từ Redis, chuyển sang lấy từ database");
            existingExamPaper = await GetExamFromDatabaseAsync(studentExamSessionDto.ShuffledExamPaperId.Value);
        }
        if (!studentExamSessionDto.OriginalExamPaperId.HasValue && existingExamPaper != null)
        {
            studentExamSessionDto.OriginalExamPaperId = existingExamPaper.OriginalExamPaperId;
            await _sessionCacheHelper.UpdateStudentExamSessionAsync(studentCode, studentExamSessionDto);
        }
        var originalExamPapers = await GetOriginalExamPaperAsync(existingExamPaper.OriginalExamPaperId);
        return (studentExamSessionDto, existingExamPaper, originalExamPapers);
    }
    #endregion

    #region GetStudentExamSessionAndOriginalPaperAsync
    // Bắt đầu thi nhưng chỉ lấy đề gốc, không random đề hoán vị
    public async Task<(StudentExamSessionCacheDto studentExamSessionDto, OriginalExamPaperDto? originalExamPaper)> GetStudentExamSessionAndOriginalPaperAsync(string studentCode, int studentExamSessionId)
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

        if (currentTime < examStartTime)
        {
            var minutesEarly = Math.Abs(timeDifference.TotalMinutes);
            _logger.LogWarning("Sinh viên {StudentCode} cố gắng thi sớm {Minutes} phút. Thời gian bắt đầu: {StartTime}, Thời gian hiện tại: {CurrentTime}", 
                studentCode, minutesEarly, examStartTime, currentTime);
            throw new InvalidOperationException($"Chưa đến thời gian làm bài. Ca thi bắt đầu lúc {examStartTime:HH:mm dd/MM/yyyy}");
        }

        if (!studentExamSessionDto.StartTime.HasValue)
        {
            // Không cho phép vào muộn quá 15 phút lần đầu
            if (timeDifference.TotalMinutes > 15)
            {
                _logger.LogWarning("Sinh viên {StudentCode} cố gắng thi muộn {Minutes} phút. Thời gian bắt đầu: {StartTime}, Thời gian hiện tại: {CurrentTime}", 
                    studentCode, timeDifference.TotalMinutes, examStartTime, currentTime);
                throw new InvalidOperationException($"Đã quá thời gian cho phép bắt đầu làm bài. Ca thi bắt đầu lúc {examStartTime:HH:mm dd/MM/yyyy}, chỉ được muộn tối đa 15 phút");
            }

            studentExamSessionDto.StartTime = DateTimeHelper.GetVietnamTime();
            
            // Lưu vào DB thông qua RabbitMQ để đảm bảo StartTime không bị mất
            var startExamMessage = new StartExamMessage
            {
                StudentExamSessionId = studentExamSessionId,
                StudentCode = studentCode,
                StartTime = studentExamSessionDto.StartTime.Value,
                ShuffledExamPaperId = studentExamSessionDto.ShuffledExamPaperId ?? 0,
                OriginalExamPaperId = studentExamSessionDto.OriginalExamPaperId ?? 0,
                StudentAnswersString = studentExamSessionDto.StudentAnswersString ?? "",
                IsCompleted = false,
                RemainingMinutes = studentExamSessionDto.RemainingMinutes
            };

            try
            {
                _rabbitMqService.Publish("start_exam_queue", startExamMessage);
                _logger.LogInformation("Đã gửi start_exam_message cho đề gốc/không hoán vị của sinh viên {studentCode}", studentCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ publish thất bại cho đề gốc của {studentCode}", studentCode);
            }
        }

        // Cập nhật RemainingMinutes
        var minutesPassed = (int)(DateTimeHelper.GetVietnamTime() - studentExamSessionDto.ExamSessionStartTime).TotalMinutes;
        studentExamSessionDto.RemainingMinutes = (studentExamSessionDto.Duration + studentExamSessionDto.ExtraMinutes) - minutesPassed;
        

        await _sessionCacheHelper.UpdateStudentExamSessionAsync(studentCode, studentExamSessionDto);

        // Lấy đề gốc
        OriginalExamPaperDto? originalExamPaper = null;
        if (studentExamSessionDto.OriginalExamPaperId.HasValue)
        {
            originalExamPaper = await GetOriginalExamPaperAsync(studentExamSessionDto.OriginalExamPaperId.Value);
        }

        return (studentExamSessionDto, originalExamPaper);
    }
    #endregion
    
    #region CreateNewExamPaperAsync
    
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
    
    #endregion
    
    #region GetRandomExamPaperAsync
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
    
    #endregion



    #region TryGetExamFromCacheAsync
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
    #endregion



    #region GetRandomPaperIdFromCacheAsync
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
    #endregion
    
    
      
    // Cache danh sách ShuffledExamPaperId theo OriginalExamPaperId
    public async Task CacheAvailablePapersAsync(int originalExamPaperId, List<ShuffledExamPaper> papers)
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
        // Format hiện tại: (1:1);(2:6);(3:10);(4:13);(5:20);...
        // Key: ID câu hỏi, Value: ID đáp án đúng (số)
        
        if (string.IsNullOrWhiteSpace(answerKeyString))
            return new Dictionary<string, string>();
            
        try
        {
            return answerKeyString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim('(', ')').Split(':'))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi parse answer key: {AnswerKeyString}", answerKeyString);
            return new Dictionary<string, string>();
        }
    }
    
    private string CreateEmptyAnswersString(string answerKey)
    {
        if (string.IsNullOrWhiteSpace(answerKey))
            return "";

        // Format hiện tại: (1:1);(2:6);(3:10);(4:13);(5:20);...
        // Tạo chuỗi đáp án rỗng bằng cách thay thế tất cả các ID đáp án sau dấu ':' bằng dấu '-'
        // Giữ nguyên cấu trúc format của answer key
        
        // Sử dụng regex để thay thế số sau dấu ':' bằng dấu '-'
        // Pattern: (?<=:)\d+  - tìm số sau dấu ':' (positive lookbehind)
        string result = Regex.Replace(answerKey, @"(?<=:)\d+", "-");
        
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

            if (!studentExamSessionDto.ShuffledExamPaperId.HasValue && !studentExamSessionDto.OriginalExamPaperId.HasValue)
            {
                _logger.LogWarning("❌ Sinh viên {StudentCode} chưa có đề thi được gán (cả hoán vị và gốc)", studentCode);
                return (false, 0, "Chưa có đề thi được gán", null, null);
            }

            string? answerKey = null;
            int? shuffledId = studentExamSessionDto.ShuffledExamPaperId;

            // Ưu tiên lấy từ đề hoán vị nếu có
            if (shuffledId.HasValue && shuffledId.Value > 0)
            {
                var (s, paperDto) = await GetExamFromRedisAsync(shuffledId.Value);
                if (paperDto == null)
                {
                    paperDto = await GetExamFromDatabaseAsync(shuffledId.Value);
                }

                if (paperDto != null)
                {
                    answerKey = paperDto.AnswerKey;
                }
            }
            
            // Nếu không có đề hoán vị hoặc không lấy được AnswerKey, thử lấy từ đề gốc
            if (string.IsNullOrEmpty(answerKey) && studentExamSessionDto.OriginalExamPaperId.HasValue)
            {
                var originalPaper = await GetOriginalExamPaperAsync(studentExamSessionDto.OriginalExamPaperId.Value);
                if (originalPaper != null)
                {
                    answerKey = originalPaper.KeyValueList;
                }
            }

            if (string.IsNullOrEmpty(answerKey))
            {
                _logger.LogError("❌ Không thể lấy được đáp án chuẩn cho phiên thi {SessionId}", studentExamSessionId);
                return (false, 0, "Không thể lấy đáp án chuẩn", null, null);
            }

            // Tính điểm và đếm câu đúng
            // Tính điểm và đếm câu đúng
            double score = 0;
            int correctAnswers = 0;
            int totalQuestions = 0;

            // Fetch Original Paper Details for Hierarchical Scoring (Support Matching Questions)
            OriginalExamPaperDto? originalPaperFull = null;
            if (studentExamSessionDto.OriginalExamPaperId.HasValue)
            {
                originalPaperFull = await GetOriginalExamPaperAsync(studentExamSessionDto.OriginalExamPaperId.Value);
            }

            if (originalPaperFull != null && originalPaperFull.Details != null && originalPaperFull.Details.Any())
            {
                 // Use Hierarchy Scoring
                 // Need to convert DTOs to Entities or adjust Helper to accept DTOs?
                 // Helper defined with: List<OriginalExamPaperDetail> details (Entities)
                 // StartExam/SubmitExam uses DTOs. 
                 // We need to map DTOs -> Entities or Overload Helper.
                 // Overloading Helper for DTOs is cleaner.
                 
                 // Let's create Overload or Mapping.
                 // Or easier: Just map DTO to needed Entity properties in a local helper or inline.
                 // Actually CalculateScoreWithHierarchy uses: OriginalExamPaperDetailId, QuestionContent, ParentQuestionId, Order, Answers (List<Answer>).
                 
                 // DTO has: OriginalExamPaperDetailId, QuestionContent, ParentQuestionId, Order, Answers (List<AnswerDto>).
                 // Properties map 1:1. We can create a lightweight mapper or overload.
                 
                 // Quick fix: Map DTO to Entity list manually here to avoid changing Helper sig excessively?
                 // Or add overload to Helper.
                 // Let's add Overload to Helper.
                 
                 var (s, c, t) = CalculateScoreWithHierarchyDto(
                    answerKey, 
                    ParseAnswerKey(studentExamSessionDto.StudentAnswersString), // Using existing ParseAnswerKey which returns Dict
                    originalPaperFull.Details
                 );
                 score = s;
                 correctAnswers = c;
                 totalQuestions = t;
            }
            else
            {
                // Fallback: Standard Flat Scoring
                 var (s, c, t) = CalculateScore(
                    answerKey, 
                    studentExamSessionDto.StudentAnswersString,
                    _logger // Logger is field, pass logger if signature matches old one or remove if new one doesn't take it
                 );
                 score = s;
                 correctAnswers = c;
                 totalQuestions = t;
            }

            // Cập nhật thông tin phiên thi
            var endTime = DateTimeHelper.GetVietnamTime();
            studentExamSessionDto.EndTime = endTime;
            studentExamSessionDto.CorrectAnswers = correctAnswers;
            studentExamSessionDto.TotalQuestions = totalQuestions;
            studentExamSessionDto.IsCompleted = true;

            // Xóa phiên khỏi Redis sau khi nộp bài
            await _sessionCacheHelper.RemoveStudentExamSessionAsync(studentCode, studentExamSessionDto.StudentExamSessionId);

            // Tạo message để lưu vào database
            var examSubmissionMessage = new ExamSubmissionMessage
            {
                StudentCode = studentCode,
                ShuffledExamPaperId = studentExamSessionDto.ShuffledExamPaperId ?? 0,
                Score = score,
                CorrectAnswers = correctAnswers,
                TotalQuestions = totalQuestions,
                IsCompleted = true,
                EndTime = endTime,
                StudentAnswersString = studentExamSessionDto.StudentAnswersString
            };

            // Gửi message qua RabbitMQ với fallback
            try
            {
                _rabbitMqService.Publish("exam_submission_queue", examSubmissionMessage);
                _logger.LogInformation("📤 Đã gửi message nộp bài thi qua RabbitMQ cho sinh viên {StudentCode}", studentCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ publish thất bại, fallback xử lý trực tiếp exam_submission_queue cho {StudentCode}", studentCode);
                await _messageProcessingService.ProcessMessageAsync("exam_submission_queue", examSubmissionMessage);
            }

            _logger.LogInformation("✅ Hoàn thành nộp bài thi cho sinh viên {StudentCode}. Điểm: {Score}, Đúng: {CorrectAnswers}/{TotalQuestions}", 
                studentCode, score, correctAnswers, totalQuestions);

            return (true, score, $"Nộp bài thi thành công. Điểm: {score:F2}, Đúng: {correctAnswers}/{totalQuestions} câu", studentExamSessionDto, answerKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi nộp bài thi cho sinh viên {StudentCode}", studentCode);
            return (false, 0, $"Lỗi hệ thống: {ex.Message}", null, null);
        }
    }

    public (double score, int correctAnswers, int totalQuestions) CalculateScore(string studentAnswers, string answerKey, ILogger? logger = null)
    {
        // Use provided logger or class logger
        var log = logger ?? _logger;
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
                        // Handle Matching Question Logic: Check if it looks like a list of pairs (e.g. "A-1;B-2")
                        // We check if both contain '-' and require splitting.
                        bool isMatching = correctAnswer.Contains("-") && (correctAnswer.Contains(";") || correctAnswer.Contains("|"));

                        if (isMatching)
                        {
                            // Normalize and Split into sets
                            var studentPairs = studentAnswer.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim())
                                .ToHashSet();
                            var correctPairs = correctAnswer.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim())
                                .ToHashSet();

                            if (studentPairs.SetEquals(correctPairs))
                            {
                                correctCount++;
                            }
                        }
                        else
                        {
                            // Standard String Comparison
                            string normStudent = studentAnswer.Replace('|', ';');
                            string normCorrect = correctAnswer.Replace('|', ';');

                            if (normStudent == normCorrect)
                            {
                                correctCount++;
                            }
                        }
                    }
                }
            }

            double score = totalCount > 0 ? (double)correctCount / totalCount * 10 : 0;

            _logger.LogInformation("📊 Kết quả tính điểm (Flat): Đúng {CorrectCount}/{TotalCount}, Điểm: {Score:F2}",
                correctCount, totalCount, score);

            return (score, correctCount, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi tính điểm");
            return (0, 0, 0);
        }
    }

    public (double Score, int CorrectAnswers, int TotalQuestions) CalculateScoreWithHierarchy(
            string answerKey,
            Dictionary<string, string> studentAnswerPairs,
            List<OriginalExamPaperDetail> details)
        {
            // Parse Answers (Legacy/Child Answers)
            var correctAnswerPairs = ParseAnswerKey(answerKey);

            int correctCount = 0;
            int totalCount = 0;

            // Group by Parent to handle Hierarchy
            // Top-level questions or Questions that are parents
            // Note: details list might be flat, we need to identify roots.
            // A matching question is a Parent.
            
            // 1. Identify Questions involved in scoring.
            // We iterate through "Question Containers".
            // If a question is a child, it is scored as part of its parent IF the parent is a Matching Question.
            // Otherwise, handled individually? 
            // Current flat logic counts every entry in answerKey.
            // Changing to Hierarchy logic requires careful count.
            
            // BETTER STRATEGY: Iterate through the ANSWER KEY keys? 
            // No, because Matching Parent doesn't have a key in AnswerKey, only children do.
            // But Student Answer has Parent Key.
            
            // Strategy: Iterate through Details (Hierarchy)
            var processedDetailIds = new HashSet<int>();
            
            // We assume details are all questions in this exam variant.
            foreach (var detail in details)
            {
                if (processedDetailIds.Contains(detail.OriginalExamPaperDetailId)) continue;

                // Check if Matching Question
                bool isMatching = !string.IsNullOrEmpty(detail.QuestionContent) && 
                                  detail.QuestionContent.Contains("[columnA]") && 
                                  detail.QuestionContent.Contains("[columnB]");

                if (isMatching)
                {
                    // Processing Matching Question (Parent)
                    processedDetailIds.Add(detail.OriginalExamPaperDetailId);
                    totalCount++; // Counts as 1 question container?
                    // NOTE: Previous logic counted sub-questions. If we change totalCount here, Score changes.
                    // Frontend 'TotalQuestions' is now Container Count. So scoring should align?
                    // User complained "Not Scored". If we align Score Calculation to Container Count:
                    // 1 Matching Question = 1 Point (or weighted?). Default 1.
                    
                    // RECONSTRUCT CORRECT ANSWER FOR PARENT
                    // Get Children from the ChildQuestions property (since 'details' list only contains Roots/Parents)
                    var children = detail.ChildQuestions != null 
                        ? detail.ChildQuestions.OrderBy(d => d.Order).ToList() 
                        : new List<OriginalExamPaperDetail>();

                    // Note: No need to add children to processedDetailIds manually if we are only iterating Roots in the main loop.
                    // But if 'details' flat list logic is assumed elsewhere, we might need care.
                    // Since 'details' passed in comes from DTO.Details (Roots only), we are fine.
                    
                    // Build Correct Pattern: A-1|B-2
                    var correctParts = new List<string>();
                    
                    // Need to parse Left Column to get Labels A, B...?
                    // MatchQuestionHelper logic: Left Index -> Right Index.
                    // Here we have Child ID -> Answer ID -> Answer Order.
                    
                    // Parse Left Column Count to map Index to Child
                    int childIndex = 0;
                    foreach (var child in children)
                    {
                        // Get Correct Answer ID from Answer Key (if any)
                        // key in dict is string "childId"
                        if (correctAnswerPairs.TryGetValue(child.OriginalExamPaperDetailId.ToString(), out var ansIdStr))
                        {
                            if (int.TryParse(ansIdStr, out int ansId))
                            {
                                // Find Answer Entity with this ID in child.Answers
                                var ans = child.Answers.FirstOrDefault(a => a.AnswerId == ansId);
                                if (ans != null)
                                {
                                    // Right Index = Order - 1 (Assuming Order 1..N)
                                    // Left Label? We can just use "A", "B" etc based on childIndex.
                                    // Or fetch from content if needed.
                                    // Frontend reconstruction uses extractLabel. 
                                    // For backend simpilicity: Let's assume standard A, B, C... maps to 0, 1, 2...
                                    // Or better: Use the SAME comparison logic as frontend?
                                    // Frontend sends "A-1|B-2".
                                    // We construct "A-1|B-2".
                                    
                                    // Helper to get Char from Index (0->A, 1->B)
                                    string leftLabel = GetLabelFromIndex(childIndex);
                                    string rightLabel = ans.Order.ToString(); 
                                    // Note: Frontend might send "1. Item" or just "1". 
                                    // Frontend fix uses "1", "2" (index+1).
                                    
                                    correctParts.Add($"{leftLabel}-{rightLabel}");
                                }
                            }
                        }
                        childIndex++;
                    }
                    
                    var correctReconstructed = string.Join("|", correctParts);
                    var correctSet = correctParts.ToHashSet();

                    // Get Student Answer
                    if (studentAnswerPairs.TryGetValue(detail.OriginalExamPaperDetailId.ToString(), out var studentAnsRaw))
                    {
                        // Student Ans: A-1|B-2
                         var studentSet = studentAnsRaw.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim())
                                .ToHashSet();
                        
                        _logger.LogInformation("MATCHING CHECK: ParentID={ParentID} Correct='{Correct}' Student='{Student}'", 
                            detail.OriginalExamPaperDetailId, correctReconstructed, studentAnsRaw);

                        // Compare Sets
                         if (correctSet.Count > 0 && 
                             studentSet.Count == correctSet.Count && 
                             studentSet.SetEquals(correctSet))
                         {
                             correctCount++;
                             _logger.LogInformation("MATCHING: CORRECT");
                         }
                         else
                         {
                             _logger.LogInformation("MATCHING: INCORRECT CorrectSetCount={C} StudentSetCount={S}", correctSet.Count, studentSet.Count);
                         }
                    }
                }
                else
                {
                    // Regular Question or Child of non-matching (shouldn't happen if looped correctly)
                    // If it has children (Group Question?), handle children?
                    // Logic: If ParentId is null, check children.
                    
                    if (detail.ParentQuestionId == null)
                    {
                        var children = detail.ChildQuestions != null ? detail.ChildQuestions.ToList() : new List<OriginalExamPaperDetail>();
                        if (children.Any())
                        {
                           // Not Matching, but Group. Score each child individually?
                           // Or Score as 1?
                           // Existing Logic was Flat.
                           // If we want to support "TotalQuestions" as Containers, we should probably score by container.
                           // BUT: Reading/Listening groups usually score per sub-question.
                           // Matching is unique because user answers ONCE for the whole block.
                           
                           // If regular group: Treat children as individual score-able items.
                           // Do NOT add to processedDetailIds of children here, let loop handle or process them now?
                           // Let's process valid AnswerKey entries only.
                           
                           processedDetailIds.Add(detail.OriginalExamPaperDetailId); // Process parent
                           // But parent itself might not have answer.
                           
                           // Loop children will be picked up by main loop? 
                           // Yes, assuming details list contains children.
                           // So we just continue.
                        }
                        else
                        {
                            // Single Question
                            processedDetailIds.Add(detail.OriginalExamPaperDetailId);
                            // Check Answer
                             if (CheckAnswer(detail.OriginalExamPaperDetailId.ToString(), studentAnswerPairs, correctAnswerPairs))
                             {
                                 correctCount++;
                             }
                             totalCount++;
                        }
                    }
                    else
                    {
                        // Is a Child Question.
                        // If Parent was Matching, it's already processed.
                        // If Parent was Group (Reading), it is processed here individually.
                        processedDetailIds.Add(detail.OriginalExamPaperDetailId);
                        
                         if (CheckAnswer(detail.OriginalExamPaperDetailId.ToString(), studentAnswerPairs, correctAnswerPairs))
                         {
                             correctCount++;
                         }
                         totalCount++;
                    }
                }
            }

             double score = totalCount > 0 ? (double)correctCount / totalCount * 10 : 0;

            _logger.LogInformation("📊 Kết quả tính điểm (Hierarchy): Đúng {CorrectCount}/{TotalCount}, Điểm: {Score:F2}",
                correctCount, totalCount, score);

            return (score, correctCount, totalCount);
        }
        
        public (double Score, int CorrectAnswers, int TotalQuestions) CalculateScoreWithHierarchyDto(
            string answerKey,
            Dictionary<string, string> studentAnswerPairs,
            List<OriginalExamPaperDetailDto> detailsDto)
        {
            // Map DTO to Entity for reuse logic
            // We only need specific fields.
            // Mapping List<AnswerDto> to List<Answer>
            
            var details = detailsDto.Select(d => new OriginalExamPaperDetail
            {
                OriginalExamPaperDetailId = d.OriginalExamPaperDetailId,
                QuestionContent = d.QuestionContent,
                ParentQuestionId = d.ParentQuestionId,
                Order = d.Order,
                Answers = d.Answers?.Select(a => new Answers
                {
                    AnswerId = a.AnswerId,
                    Order = a.Order,
                    AnswerContent = a.AnswerContent
                    // Other fields not needed for scoring
                }).ToList() ?? new List<Answers>(),
                
                // CRITICAL FIX: Map ChildQuestions for Hierarchy Logic
                ChildQuestions = d.ChildQuestions?.Select(c => new OriginalExamPaperDetail 
                {
                    OriginalExamPaperDetailId = c.OriginalExamPaperDetailId,
                    Order = c.Order,
                    ParentQuestionId = c.ParentQuestionId,
                    QuestionContent = c.QuestionContent,
                    Answers = c.Answers?.Select(a => new Answers 
                    {
                        AnswerId = a.AnswerId,
                        Order = a.Order,
                        AnswerContent = a.AnswerContent
                    }).ToList() ?? new List<Answers>()
                }).ToList() ?? new List<OriginalExamPaperDetail>()
                
            }).ToList();

            return CalculateScoreWithHierarchy(answerKey, studentAnswerPairs, details);
        }
        private string GetLabelFromIndex(int index)
        {
            // 0 -> A, 1 -> B ...
            return ((char)('A' + index)).ToString();
        }

        private bool CheckAnswer(string key, Dictionary<string,string> studentPairs, Dictionary<string,string> correctPairs)
        {
             if (correctPairs.TryGetValue(key, out var correctAns))
             {
                 if (studentPairs.TryGetValue(key, out var studentAns))
                 {
                    if (!string.IsNullOrWhiteSpace(studentAns) && studentAns != "-")
                    {
                          string normStudent = studentAns.Replace('|', ';');
                          string normCorrect = correctAns.Replace('|', ';');
                          return normStudent == normCorrect;
                    }
                 }
             }
             return false;
        }

    public async Task<OriginalExamPaperDto> GetWithDetailsByIdAsync(int originalExamPaperId)
    {
        var examPaper = await _originalExamPaperRepository.GetQueryable()
            .Where(x => x.OriginalExamPaperId == originalExamPaperId)
            .Include(x => x.OriginalExamPaperDetails)
                .ThenInclude(d => d.Answers)
            .Include(x => x.OriginalExamPaperDetails)
                .ThenInclude(d => d.ChildQuestions)
                    .ThenInclude(c => c.Answers)
            .FirstOrDefaultAsync();
        if (examPaper == null) return null;
        
        var examPaperDto = _mapper.Map<OriginalExamPaperDto>(examPaper);
        
        // Cache vào Redis
        await CacheOriginalExamPaperAsync(originalExamPaperId, examPaperDto);
        
        return examPaperDto;
    }
    
    public async Task<OriginalExamPaperDto> GetOriginalExamPaperAsync(int originalExamPaperId)
    {
        try
        {
            // Thử lấy từ Redis trước
            var (success, examPaperDto) = await GetOriginalExamPaperFromRedisAsync(originalExamPaperId);
            if (success && examPaperDto != null)
            {
                _logger.LogInformation("Đã lấy được đề thi gốc {OriginalExamPaperId} từ Redis", originalExamPaperId);
                return examPaperDto;
            }
            
            // Nếu không có trong Redis, lấy từ database
            _logger.LogInformation("Không tìm thấy đề thi gốc {OriginalExamPaperId} trong Redis, chuyển sang lấy từ database", originalExamPaperId);
            return await GetWithDetailsByIdAsync(originalExamPaperId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy đề thi gốc {OriginalExamPaperId}", originalExamPaperId);
            throw;
        }
    }
    
    private async Task<(bool success, OriginalExamPaperDto? examPaperDto)> GetOriginalExamPaperFromRedisAsync(int originalExamPaperId)
    {
        try
        {
            var db = _redisService.GetDatabase();
            string cacheKey = $"original_exam_paper_v2:{originalExamPaperId}";
            _logger.LogInformation("Đang tìm đề thi gốc từ Redis với key: {CacheKey}", cacheKey);

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
                    var paperDto = JsonSerializer.Deserialize<OriginalExamPaperDto>(cachedValue, jsonOptions);
                    if (paperDto != null)
                    {
                        _logger.LogInformation("Đã lấy được đề thi gốc từ Redis");
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
                    _logger.LogError(ex, "Lỗi khi deserialize đề thi gốc từ Redis, xóa key: {CacheKey}", cacheKey);
                    await db.KeyDeleteAsync(cacheKey);
                    return (false, null);
                }
            }
            else
            {
                _logger.LogInformation("Không tìm thấy đề thi gốc trong Redis với key: {CacheKey}", cacheKey);
                return (false, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi truy cập Redis để lấy đề thi gốc");
            return (false, null);
        }
    }


    #region CacheOriginalExamPaperAsync
    
    private async Task CacheOriginalExamPaperAsync(int originalExamPaperId, OriginalExamPaperDto examPaperDto)
    {
        try
        {
            // Kiểm tra Redis connection trước khi cache
            if (!_redisService.IsConnected)
            {
                _logger.LogWarning("Redis không khả dụng, bỏ qua cache đề thi gốc vào Redis cho {OriginalExamPaperId}", 
                    originalExamPaperId);
                return;
            }

            var db = _redisService.GetDatabase();
            string cacheKey = $"original_exam_paper_v2:{originalExamPaperId}";
            
            var jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve,
                MaxDepth = 64
            };
            var jsonString = JsonSerializer.Serialize(examPaperDto, jsonOptions);
            await db.StringSetAsync(cacheKey, jsonString, TimeSpan.FromHours(6));
            
            _logger.LogInformation("Đã cache đề thi gốc {OriginalExamPaperId} vào Redis", originalExamPaperId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cache đề thi gốc {OriginalExamPaperId} vào Redis", originalExamPaperId);
        }
    }
        

    #endregion
    
} 