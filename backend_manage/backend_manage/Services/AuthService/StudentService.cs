using backend_manage.Entities;
using backend_manage.Repositories;
using backend_manage.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend_manage.Repositories.Interfaces;
using OfficeOpenXml;
using backend_manage.Services.Interfaces;
using backend_manage.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using AutoMapper;
using backend_manage.Extensions;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using backend_manage.Messages;
using backend_manage.Messages.RabbitMQ;
using Newtonsoft.Json;
using System.Text.Json;
using System.Linq;
using backend_manage.Services.AuthService.Helpers;

namespace backend_manage.Services.AuthService;

public class StudentService : IStudentService
{
    private readonly IRepository<Student> _repository;
    private readonly IRepository<StudentExamSession> _studentExamSessionRepository;
    private readonly IRepository<ExamSessionSubject> _examSessionSubjectRepository;
    private readonly IRepository<ShuffledExamPaper> _shuffledExamPaperRepository;
    private readonly IRepository<ExamRoom> _examRoomRepository;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<StudentService> _logger;
    private readonly IRabbitMqService _rabbitMQService;
    private readonly StudentCacheHelper _studentCacheHelper;
    private readonly StudentExamSessionCacheHelper _sessionCacheHelper;
    private readonly ExamPaperHelper _examPaperHelper;
    private readonly StudentAnswerHelper _answerHelper;
    private readonly StudentValidationHelper _validationHelper;
    private readonly StudentImportHelper _importHelper;

    public StudentService(
        IRepository<Student> repository,
        IRepository<StudentExamSession> studentExamSessionRepository,
        IRepository<ExamSessionSubject> examSessionSubjectRepository,
        IRepository<ShuffledExamPaper> shuffledExamPaperRepository,
        IRepository<ExamRoom> examRoomRepository,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IHubContext<NotificationHub> hubContext,
        IConnectionMultiplexer redis,
        ILogger<StudentService> logger,
        IRabbitMqService rabbitMQService,
        StudentCacheHelper studentCacheHelper,
        StudentExamSessionCacheHelper sessionCacheHelper,
        ExamPaperHelper examPaperHelper,
        StudentAnswerHelper answerHelper,
        StudentValidationHelper validationHelper,
        StudentImportHelper importHelper)
    {
        _repository = repository;
        _studentExamSessionRepository = studentExamSessionRepository;
        _examSessionSubjectRepository = examSessionSubjectRepository;
        _shuffledExamPaperRepository = shuffledExamPaperRepository;
        _examRoomRepository = examRoomRepository;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _mapper = mapper;
        _hubContext = hubContext;
        _redis = redis;
        _logger = logger;
        _rabbitMQService = rabbitMQService;
        _studentCacheHelper = studentCacheHelper;
        _sessionCacheHelper = sessionCacheHelper;
        _examPaperHelper = examPaperHelper;
        _answerHelper = answerHelper;
        _validationHelper = validationHelper;
        _importHelper = importHelper;
    }

    //đã tối ưu
    #region LoginAsync
    public async Task<StudentAuthResultDto> LoginAsync(string studentCode1, string studentCode2)
    {
        if (studentCode1 != studentCode2)
        {
            return new StudentAuthResultDto
            {
                IsSuccess = false,
                ErrorMessage = "Mã sinh viên nhập không khớp."
            };
        }
        
        // Sử dụng GetStudentFromRedisAsync thay vì truy vấn database trực tiếp
        var student = await _studentCacheHelper.GetStudentFromRedisAsync(studentCode1);
        
        if (student == null)
        {
            return new StudentAuthResultDto
            {
                IsSuccess = false,
                ErrorMessage = "Không tìm thấy sinh viên với mã này."
            };
        }
        /*
        if (student.IsLogin)
        {
            return new StudentAuthResultDto
            {
                IsSuccess = false,
                ErrorMessage = "Sinh viên đã có phiên đăng nhập."
            };
        }*/
        /*
        student.IsLogin = true;
        student.LastLoggedIn = DateTimeHelper.GetVietnamTime();
        await _repository.UpdateAsync(student);
        */ //tạm thời không dùng tới giới hạn phiên đăng nhập và thời gian đăng nhập lần cuối
        // Cập nhật lại vào Redis cache sau khi thay đổi
        await _studentCacheHelper.UpdateStudentInRedisAsync(student);

        // Sinh JWT token như cũ, nhưng không có username
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["JWT:key"] ?? "default_secret_key");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("id", student.StudentId.ToString()),
                new Claim("studentCode", student.StudentCode),
                new Claim("role", "Student")
            }),
            Expires = DateTimeHelper.GetVietnamTime().AddDays(7),
            Issuer = _configuration["JWT:Issuer"],
            Audience = _configuration["JWT:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);
        return new StudentAuthResultDto
        {
            IsSuccess = true,
            Token = tokenString,
            Role = "Student",
            StudentInfo = new
            {
                student.StudentId,
                student.StudentCode,
                student.FirstName,
                student.LastName,
                student.Gender,
                student.DateOfBirth
            }
        };
    }
    #endregion  

    //không dùng tới nhiều nên chưa cải thiện
    #region GetAllAsync 
    public async Task<IEnumerable<Student>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }
    #endregion 

    //đã tối ưu (để xem thông tin)
    #region GetByStudentCodeAsync
    public async Task<Student?> GetByStudentCodeAsync(string studentCode)
    {
        return await _studentCacheHelper.GetStudentFromRedisAsync(studentCode);
    }
    #endregion
    
    
    //không dùng tới nhiều nên chưa cải thiện
    #region AddAsync
    public async Task<Student> AddAsync(StudentCreateDto dto)
    {
        var student = new Student
        {
            StudentCode = dto.StudentCode,
            FirstName = dto.FirstName,
            LastName = dto.LastName
        };
        return await _repository.AddAsync(student);
    }
    #endregion
    
    //đã tối ưu
    #region ImportFromExcelAsync
    public async Task<StudentImportResultDto> ImportFromExcelAsync(IFormFile file, string examSessionSubjectCore, int examRoomId)
    {
        if (file == null || file.Length == 0)
            return new StudentImportResultDto { StudentsAdded = 0, StudentExamSessionsAdded = 0 };
            
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("Không thể xác định người dùng tạo sinh viên.");

        // Tạo Job ID
        var jobId = Guid.NewGuid().ToString();
        
        // Convert file thành base64
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var fileContent = Convert.ToBase64String(ms.ToArray());
        
        // Tạo message
        var message = new StudentImportMessage
        {
            JobId = jobId,
            FileContent = fileContent,
            FileName = file.FileName,
            ExamSessionSubjectCore = examSessionSubjectCore,
            ExamRoomId = examRoomId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        
        // Gửi message vào RabbitMQ
        _rabbitMQService.PublishMessage("student_import_queue", message);
        
        _logger.LogInformation("Đã gửi import job {JobId} vào queue. File: {FileName}, ExamSessionSubjectCore: {Core}", 
            jobId, file.FileName, examSessionSubjectCore);
        
        // Trả về kết quả tạm thời (sẽ được cập nhật bởi consumer)
        return new StudentImportResultDto 
        { 
            StudentsAdded = 0, 
            StudentExamSessionsAdded = 0,
            JobId = jobId // Thêm JobId để frontend có thể track progress
        };
    }
    #endregion
    
    //đã tối ưu
    #region ImportFromExcelStreamAsync
    public async Task<StudentImportResultDto> ImportFromExcelStreamAsync(Stream stream, string examSessionSubjectCore, int examRoomId, string userId)
    {
        return await _importHelper.ImportFromExcelStreamAsync(stream, examSessionSubjectCore, examRoomId, userId);
    }
    #endregion

    
    #region UpdateAsync
    public async Task<Student> UpdateAsync(string id, Student student)
    {
        return await _repository.UpdateAsync(student);
    }
    #endregion

    #region DeleteAsync
    public async Task<bool> DeleteAsync(string id)
    {
        return await _repository.DeleteAsync(id);
    }
    #endregion

    
    //Có thể tối ưu hơn // chuẩn bị tối ưu bằng cách lưu chuỗi đáp án của sinh viên vào phiên thi tránh lưu nhiều key vào redis
    #region StartExamAsync
    public async Task<ShuffledExamPaperDto> StartExamAsync(string studentCode, int studentExamSessionId)
    {
        _logger.LogInformation("Bắt đầu lấy đề thi cho sinh viên {StudentCode}, phiên thi {StudentExamSessionId}", 
            studentCode, studentExamSessionId);

        // 1. Lấy và validate StudentExamSession
        var studentExamSession = await GetAndValidateStudentExamSessionAsync(studentCode, studentExamSessionId);
        
        // 2. Lấy đề thi (từ cache hoặc tạo mới)
        var paperDto = await GetOrCreateExamPaperAsync(studentCode, studentExamSession);
        
        // 3. Nếu là đề thi mới, khởi tạo và cache
        if (studentExamSession.ShuffledExamPaperId == null)
        {
            await InitializeNewExamSessionAsync(studentCode, studentExamSession, paperDto);
        }

        _logger.LogInformation("Hoàn thành quá trình lấy đề thi cho sinh viên {StudentCode}, mã đề: {ShuffledExamPaperId}, số câu hỏi: {QuestionCount}", 
            studentCode, paperDto.ShuffledExamPaperId, paperDto.Details?.Count ?? 0);
            
        return paperDto;
    }

    private async Task<StudentExamSession> GetAndValidateStudentExamSessionAsync(string studentCode, int studentExamSessionId)
    {
        var cachedSessionDto = await _sessionCacheHelper.GetStudentExamSessionFromCacheAsync(studentCode, studentExamSessionId);
        
        if (cachedSessionDto == null)
        {
            _logger.LogError("Không tìm thấy phiên thi của sinh viên {StudentCode}", studentCode);
            throw new Exception("Không tìm thấy phiên thi của sinh viên.");
        }

        return _mapper.Map<StudentExamSession>(cachedSessionDto);
    }

    private async Task<ShuffledExamPaperDto> GetOrCreateExamPaperAsync(string studentCode, StudentExamSession studentExamSession)
    {
        var shuffledExamPaperId = studentExamSession.ShuffledExamPaperId;
        var examSessionSubjectId = studentExamSession.ExamSessionSubjectId;

        // Nếu đã có đề thi, thử lấy từ cache trước
        if (shuffledExamPaperId.HasValue)
        {
            var paperDto = await TryGetExamFromCacheAsync(shuffledExamPaperId.Value, studentCode);
            if (paperDto != null) return paperDto;
            
            // Fallback về database
            return await GetExamFromDatabaseAsync(shuffledExamPaperId.Value);
        }

        // Tạo đề thi mới
        return await CreateNewExamPaperAsync(examSessionSubjectId, studentCode);
    }

    private async Task<ShuffledExamPaperDto?> TryGetExamFromCacheAsync(int shuffledExamPaperId, string studentCode)
    {
        try
        {
            _logger.LogInformation("Thử lấy đề thi {ShuffledExamPaperId} từ Redis", shuffledExamPaperId);
            var paperDto = await _examPaperHelper.GetExamFromRedisAsync(shuffledExamPaperId);
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

    private async Task<ShuffledExamPaperDto> GetExamFromDatabaseAsync(int shuffledExamPaperId)
    {
        _logger.LogInformation("Lấy đề thi từ database cho ShuffledExamPaperId: {ShuffledExamPaperId}", shuffledExamPaperId);
        var (examPaper, examPaperDto) = await _examPaperHelper.GetExamFromDatabaseAsync(shuffledExamPaperId);
        
        // Cache lại vào Redis
        await _examPaperHelper.CacheExamPaperAsync(shuffledExamPaperId, examPaperDto, examPaper.AnswerKey);
        
        return examPaperDto;
    }

    private async Task<ShuffledExamPaperDto> CreateNewExamPaperAsync(int examSessionSubjectId, string studentCode)
    {
        var shuffledExamPaper = await _examPaperHelper.GetRandomExamPaperAsync(examSessionSubjectId);
        
        _logger.LogInformation("Đã chọn ngẫu nhiên đề thi {ShuffledExamPaperId} cho sinh viên {StudentCode}", 
            shuffledExamPaper.ShuffledExamPaperId, studentCode);
        
        // Thử lấy đề từ cache trước, fallback về database
        var paperDto = await TryGetExamFromCacheAsync(shuffledExamPaper.ShuffledExamPaperId, studentCode);
        if (paperDto == null)
        {
            var (examPaper, examPaperDto) = await _examPaperHelper.GetExamFromDatabaseAsync(shuffledExamPaper.ShuffledExamPaperId);
            shuffledExamPaper = examPaper;
            paperDto = examPaperDto;
        }
        
        return paperDto;
    }

    private async Task InitializeNewExamSessionAsync(string studentCode, StudentExamSession studentExamSession, ShuffledExamPaperDto paperDto)
    {
        // Cập nhật ShuffledExamPaperId vào cache
        await UpdateExamSessionInCacheAsync(studentCode, studentExamSession, paperDto.ShuffledExamPaperId);
        
        // Khởi tạo chuỗi đáp án rỗng
        var emptyAnswers = CreateEmptyAnswersString(paperDto);
        
        // Cập nhật database  // sẽ dùng rabit mq để tối ưu 
        await UpdateExamSessionInDatabaseAsync(studentExamSession, paperDto.ShuffledExamPaperId, emptyAnswers);
        
        // Cache vào Redis
        await CacheExamDataAsync(studentCode, paperDto, emptyAnswers);
    }

    private async Task UpdateExamSessionInCacheAsync(string studentCode, StudentExamSession studentExamSession, int shuffledExamPaperId)
    {
        try
        {
            studentExamSession.ShuffledExamPaperId = shuffledExamPaperId;
            await _sessionCacheHelper.CacheStudentExamSessionsForStudentAsync(studentCode, new List<StudentExamSession> { studentExamSession });
            
            _logger.LogInformation("Đã cập nhật ShuffledExamPaperId {ShuffledExamPaperId} vào StudentExamSession trong Redis cho sinh viên {StudentCode}", 
                shuffledExamPaperId, studentCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi cập nhật ShuffledExamPaperId vào StudentExamSession trong Redis cho sinh viên {StudentCode}", studentCode);
        }
    }

    private string CreateEmptyAnswersString(ShuffledExamPaperDto paperDto)
    {
        // Ước tính số câu hỏi từ paperDto
        var questionCount = paperDto.Details?.Count ?? 0;
        return string.Join(";", Enumerable.Range(1, questionCount).Select(i => $"({i},-)")) + ";";
    }

    private async Task UpdateExamSessionInDatabaseAsync(StudentExamSession studentExamSession, int shuffledExamPaperId, string emptyAnswers)
    {
        studentExamSession.StartTime = DateTimeHelper.GetVietnamTime();
        studentExamSession.ShuffledExamPaperId = shuffledExamPaperId;
        studentExamSession.StudentAnswersString = emptyAnswers;
        await _studentExamSessionRepository.UpdateAsync(studentExamSession);
        
        _logger.LogInformation("Đã cập nhật thông tin đề thi và chuỗi đáp án rỗng cho sinh viên trong database");
    }

    private async Task CacheExamDataAsync(string studentCode, ShuffledExamPaperDto paperDto, string emptyAnswers)
    {
        try
        {
            var db = _redis.GetDatabase();
            string cacheKey = $"shuffled_exam_paper:{paperDto.ShuffledExamPaperId}";
            string answerKey = $"answer_key:{paperDto.ShuffledExamPaperId}";
            string studentAnswerKey = $"student_answers:{studentCode}:{paperDto.ShuffledExamPaperId}";

            // Kiểm tra sự tồn tại của các key song song
            var existTasks = new[]
            {
                db.KeyExistsAsync(cacheKey),
                db.KeyExistsAsync(answerKey),
                db.KeyExistsAsync(studentAnswerKey)
            };
            await Task.WhenAll(existTasks);

            var examExists = await existTasks[0];
            var answerKeyExists = await existTasks[1];
            var studentAnswerExists = await existTasks[2];
            
            var jsonString = System.Text.Json.JsonSerializer.Serialize(paperDto);
            var batch = db.CreateBatch();
            var tasks = new List<Task>();

            // Chỉ cache những key chưa tồn tại
            if (!examExists)
            {
                _logger.LogInformation("Cache đề thi mới vào Redis");
                tasks.Add(batch.StringSetAsync(cacheKey, jsonString, TimeSpan.FromHours(6)));
            }

            if (!answerKeyExists)
            {
                _logger.LogInformation("Cache answer key mới vào Redis");
                tasks.Add(batch.StringSetAsync(answerKey, paperDto.AnswerKey, TimeSpan.FromHours(6)));
            }

            if (!studentAnswerExists)
            {
                _logger.LogInformation("Cache student answers mới vào Redis");
                tasks.Add(batch.StringSetAsync(studentAnswerKey, emptyAnswers, TimeSpan.FromHours(6)));
            }
            
            if (tasks.Any())
            {
                batch.Execute();
                await Task.WhenAll(tasks);
                _logger.LogInformation("Đã hoàn thành cache dữ liệu mới vào Redis");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cache dữ liệu vào Redis");
        }
    }
    #endregion
   




    #region GetStudentExamSessionsAsync
    public async Task<IEnumerable<StudentExamSessionDto>> GetStudentExamSessionsAsync(string studentCode)
    {
        try
        {
            // Thử lấy từ Redis cache trước
            var cachedSessions = await _sessionCacheHelper.GetStudentExamSessionsFromRedisCacheAsync(studentCode);
            if (cachedSessions != null && cachedSessions.Any())
            {
                _logger.LogDebug("Đã lấy {Count} phiên thi từ Redis cache cho sinh viên {StudentCode}", 
                    cachedSessions.Count(), studentCode);
                return cachedSessions;
            }
            
            _logger.LogDebug("Không tìm thấy phiên thi trong Redis cache cho sinh viên {StudentCode}, kiểm tra database", studentCode);
            
            // Fallback về database
            var student = await _repository.GetQueryable().FirstOrDefaultAsync(x => x.StudentCode == studentCode);
            if (student == null) return Enumerable.Empty<StudentExamSessionDto>();
            
            var sessions = await _studentExamSessionRepository.GetQueryable()
                .Where(x => x.StudentId == student.StudentId && x.IsCompleted == false)
                .Include(x => x.ExamSessionSubject)
                    .ThenInclude(x => x.Subject)
                .Include(x => x.ExamRoom)
                .ToListAsync();
            
            // Cache lại vào Redis
            await _sessionCacheHelper.CacheStudentExamSessionsForStudentAsync(studentCode, sessions);
            
            return sessions.Select(x => _mapper.Map<StudentExamSessionDto>(x));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy phiên thi cho sinh viên {StudentCode}", studentCode);
            return Enumerable.Empty<StudentExamSessionDto>();
        }
    }
    #endregion



    #region GetStudentsByExamRoomAsync
    public async Task<IEnumerable<StudentExamRoomStatusDto>> GetStudentsByExamRoomAsync(int examRoomId, int examSessionSubjectId)
    {
        var sessions = await _studentExamSessionRepository.GetQueryable()
            .Where(ses => ses.ExamRoomId == examRoomId && ses.ExamSessionSubjectId == examSessionSubjectId)
            .Include(ses => ses.Student)
            .Include(ses => ses.ExamSessionSubject)
                .ThenInclude(ess => ess.Subject)
            .ToListAsync();
        return sessions.Select(x => _mapper.Map<StudentExamRoomStatusDto>(x));
    }
    #endregion
    
    #region AddExtraMinutesAsync
    public async Task<bool> AddExtraMinutesAsync(string studentCode, int studentExamSessionId, int extraMinutes, string? reasonForExtra)
    {
        // 1. Tìm StudentExamSession cần cập nhật
        var session = await _studentExamSessionRepository.GetQueryable()
            .Include(s => s.Student)
            .FirstOrDefaultAsync(s => s.StudentExamSessionId == studentExamSessionId && s.StudentCode == studentCode);

        if (session == null)
            throw new Exception("Không tìm thấy phiên thi sinh viên với mã đã cung cấp.");

        // 2. Cập nhật ExtraMinutes và ReasonForExtra
        session.ExtraMinutes = extraMinutes;
        session.ReasonForExtra = reasonForExtra;

        // 3. Cập nhật UpdatedAt, UpdatedBy nếu có thông tin từ context
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        session.UpdatedBy = userId;
        session.UpdatedAt = DateTimeHelper.GetVietnamTime();

        // 4. Lưu vào DB
        await _studentExamSessionRepository.UpdateAsync(session);

        return true;
    }
    #endregion
    
    #region AvtiveLoginAsync
    public async Task<(bool Success, string Message)> AvtiveLoginAsync(string studentCode, bool isLogin)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        var student = await _repository.GetQueryable()
            .FirstOrDefaultAsync(x => x.StudentCode == studentCode);

        if (student == null)
        {
            return (false, "Không tìm thấy sinh viên với mã này.");
        }

        if (student.IsLogin == isLogin)
        {
            return (true, "Trạng thái đăng nhập đã đúng, không cần cập nhật.");
        }

        student.IsLogin = isLogin;
        student.UpdatedBy = userId;
        student.UpdatedAt = DateTimeHelper.GetVietnamTime();

        await _repository.UpdateAsync(student);

        return (true, "Cập nhật trạng thái đăng nhập thành công.");
    }
    #endregion

    // Helper methods để tái sử dụng code






    #region SubmitExamAsync
    public async Task<(bool Success, string Message, double? Score)> SubmitExamAsync(string StudentCode,SubmitExamDto submitExamDto)
    {
        try
        {
            // Tạo cache key để tái sử dụng
            string studentAnswerKey = $"student_answers:{StudentCode}:{submitExamDto.ShuffledExamPaperId}";
            string answerKey = $"answer_key:{submitExamDto.ShuffledExamPaperId}";
            string sessionCacheKey = $"student_exam_session:{StudentCode}:*";
            
            var db = _redis.GetDatabase();
            
            // Parallel processing: Lấy dữ liệu từ Redis đồng thời
            Task<RedisValue> studentAnswersTask = db.StringGetAsync(studentAnswerKey);
            Task<RedisValue> answerKeyTask = db.StringGetAsync(answerKey);
            Task<StudentExamSession?> sessionTask = _validationHelper.GetStudentExamSessionFromCacheAsync(db, sessionCacheKey, submitExamDto.ShuffledExamPaperId);
            
            await Task.WhenAll(studentAnswersTask, answerKeyTask, sessionTask);
            
            var studentAnswers = await studentAnswersTask;
            var answerKeyValue = await answerKeyTask;
            var cachedSession = await sessionTask;
            
            // Validate và lấy dữ liệu song song
            Task<(bool Success, string Message)> validationTask = _validationHelper.ValidateStudentExamSessionOptimizedAsync(StudentCode, submitExamDto.ShuffledExamPaperId, cachedSession);
            Task<(bool Success, string Message, Dictionary<int, string>? CurrentAnswers)> updateAnswersTask = _answerHelper.UpdateStudentAnswersOptimizedAsync(studentAnswers, submitExamDto.SaveAnswerDtos, studentAnswerKey, db);
            
            await Task.WhenAll(validationTask, updateAnswersTask);
            
            var (validationSuccess, validationMessage) = await validationTask;
            var (updateSuccess, updateMessage, currentAnswers) = await updateAnswersTask;
            
            if (!validationSuccess)
            {
                return (false, validationMessage, null);
            }
            
            if (!updateSuccess)
            {
                return (false, updateMessage, null);
            }
            
            // Xử lý answer key
            if (!answerKeyValue.HasValue)
            {
                _logger.LogError("Không tìm thấy đáp án trong Redis");
                return (false, "Không tìm thấy đáp án", null);
            }
            
            var correctAnswerPairs = _answerHelper.ParseAnswerKey(answerKeyValue.ToString());
            
            // Tính điểm tối ưu
            var (score, correctCount, totalQuestions) = _answerHelper.CalculateScoreOptimized(currentAnswers, correctAnswerPairs);
            
            // Tạo message và gửi RabbitMQ
            var examSubmissionMessage = _answerHelper.CreateExamSubmissionMessage(StudentCode, submitExamDto.ShuffledExamPaperId, score, correctCount, totalQuestions, currentAnswers);
            _rabbitMQService.PublishMessage("submit_exam_queue", examSubmissionMessage);
            
            _logger.LogInformation(
                "Sinh viên {StudentCode} đã nộp bài thi {ShuffledExamPaperId} với điểm {Score}", 
                StudentCode, submitExamDto.ShuffledExamPaperId, score);
            
            return (true, "Nộp bài thành công", score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi nộp bài thi của sinh viên {StudentCode}", StudentCode);
            return (false, $"Lỗi khi nộp bài: {ex.Message}", null);
        }
    }
    #endregion

    #region SaveStudentAnswerAsync
    public async Task<(bool Success, string Message)> SaveStudentAnswerAsync(string studentCode, int shuffledExamPaperId, int index, string answer)
    {
        try
        {
            // Sử dụng helper method để cập nhật đáp án đơn lẻ
            var (success, message, newAnswersString) = await _answerHelper.UpdateSingleAnswerAsync(
                studentCode, shuffledExamPaperId, index, answer);
            
            if (!success)
            {
                return (false, message);
            }
            
            // Gửi message qua RabbitMQ
            var answerSavedMessage = _answerHelper.CreateAnswerSavedMessage(studentCode, shuffledExamPaperId, index, answer, newAnswersString);
            _rabbitMQService.PublishMessage("save_answer_queue", answerSavedMessage);
            
            return (true, "Đã lưu đáp án thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu đáp án của sinh viên. StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}, Index: {Index}",
                studentCode, shuffledExamPaperId, index);
            return (false, $"Lỗi khi lưu đáp án: {ex.Message}");
        }
    }
    #endregion

    #region SaveExamAsync
    public async Task<(bool Success, string Message)> SaveExamAsync(string StudentCode, SubmitExamDto submitExamDto)
    {
        try
        {
            // Tạo cache key để tái sử dụng
            string studentAnswerKey = $"student_answers:{StudentCode}:{submitExamDto.ShuffledExamPaperId}";
            string sessionCacheKey = $"student_exam_session:{StudentCode}:*";
            
            var db = _redis.GetDatabase();
            
            // Parallel processing: Lấy dữ liệu từ Redis đồng thời
            Task<RedisValue> studentAnswersTask = db.StringGetAsync(studentAnswerKey);
            Task<StudentExamSession?> sessionTask = _validationHelper.GetStudentExamSessionFromCacheAsync(db, sessionCacheKey, submitExamDto.ShuffledExamPaperId);
            
            await Task.WhenAll(studentAnswersTask, sessionTask);
            
            var studentAnswers = await studentAnswersTask;
            var cachedSession = await sessionTask;
            
            // Validate và update answers song song
            Task<(bool Success, string Message)> validationTask = _validationHelper.ValidateStudentExamSessionOptimizedAsync(StudentCode, submitExamDto.ShuffledExamPaperId, cachedSession);
            Task<(bool Success, string Message, Dictionary<int, string>? CurrentAnswers)> updateAnswersTask = _answerHelper.UpdateStudentAnswersOptimizedAsync(studentAnswers, submitExamDto.SaveAnswerDtos, studentAnswerKey, db);
            
            await Task.WhenAll(validationTask, updateAnswersTask);
            
            var (validationSuccess, validationMessage) = await validationTask;
            var (updateSuccess, updateMessage, currentAnswers) = await updateAnswersTask;
            
            if (!validationSuccess)
            {
                return (false, validationMessage);
            }
            
            if (!updateSuccess)
            {
                return (false, updateMessage);
            }
            
            // Tạo message và gửi RabbitMQ
            var saveExamMessage = _answerHelper.CreateSaveExamMessage(StudentCode, submitExamDto.ShuffledExamPaperId, currentAnswers);
            _rabbitMQService.PublishMessage("save_exam_queue", saveExamMessage);

            _logger.LogInformation(
                "Sinh viên {StudentCode} đã lưu bài thi {ShuffledExamPaperId} thành công (Redis + RabbitMQ)", 
                StudentCode, submitExamDto.ShuffledExamPaperId);

            return (true, "Lưu bài thi thành công (Redis + RabbitMQ)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu bài thi của sinh viên {StudentCode}", StudentCode);
            return (false, $"Lỗi khi lưu bài thi: {ex.Message}");
        }
    }
    #endregion



    



} 
