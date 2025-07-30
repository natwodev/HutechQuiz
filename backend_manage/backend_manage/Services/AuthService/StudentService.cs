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
        IRabbitMqService rabbitMQService)
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
    }

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
        var students = await _repository.GetAllAsync();
        var student = students.FirstOrDefault(s => s.StudentCode == studentCode1);
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
        
        student.IsLogin = true;
        student.LastLoggedIn = DateTimeHelper.GetVietnamTime();
        await _repository.UpdateAsync(student);

        // Gửi message vào RabbitMQ để worker thực hiện cache StudentExamSession
        // Định nghĩa DTO ở file riêng, sử dụng khi gửi message vào RabbitMQ
        var cacheRequest = new CacheStudentExamSessionsMessage { StudentCode = studentCode1 };
        _rabbitMQService.PublishMessage("cache_student_exam_sessions_queue", cacheRequest);

        // (Giữ nguyên các đoạn code khác, không truy vấn studentExamSessions và không cache Redis ở đây)
        // Nếu cần gửi trạng thái phòng thi realtime, có thể cân nhắc chuyển sang worker hoặc giữ lại đoạn này nếu thực sự cần thiết

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

    #region GetAllAsync
    public async Task<IEnumerable<Student>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }
    #endregion

    #region GetByStudentCodeAsync
    public async Task<Student?> GetByStudentCodeAsync(string studentCode)
    {
        var students = await _repository.GetAllAsync();
        return students.FirstOrDefault(s => s.StudentCode == studentCode);
    }
    #endregion

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

    #region AddRangeAsync
    public async Task<IEnumerable<Student>> AddRangeAsync(IEnumerable<StudentCreateDto> dtos)
    {
        var students = dtos.Select(dto => new Student
        {
            StudentCode = dto.StudentCode,
            FirstName = dto.FirstName,
            LastName = dto.LastName
        }).ToList();
        var result = new List<Student>();
        foreach (var student in students)
        {
            var added = await _repository.AddAsync(student);
            result.Add(added);
        }
        return result;
    }
    #endregion

    #region BulkImportStudentsAsync
    public async Task<int> BulkImportStudentsAsync(List<StudentCreateDto> students)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("Không thể xác định người dùng tạo sinh viên.");
        var now = DateTimeHelper.GetVietnamTime();
        var entities = students.Select(dto => new Student
        {
            StudentCode = dto.StudentCode,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            CreatedBy = userId,
            CreatedAt = DateTimeHelper.GetVietnamTime()
        }).ToList();
        foreach (var student in entities)
        {
            await _repository.AddAsync(student);
        }
        return entities.Count;
    }
    #endregion

    #region ImportFromExcelAsync
    public async Task<StudentImportResultDto> ImportFromExcelAsync(IFormFile file, string examSessionSubjectCore, int examRoomId)
    {
        if (file == null || file.Length == 0)
            return new StudentImportResultDto { StudentsAdded = 0, StudentExamSessionsAdded = 0 };
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("Không thể xác định người dùng tạo sinh viên.");
        var students = new List<Student>();
        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;
                for (int row = 2; row <= rowCount; row++) // Bỏ qua header
                {
                    var studentCode = worksheet.Cells[row, 2].Text;
                    var firstName = worksheet.Cells[row, 3].Text;
                    var lastName = worksheet.Cells[row, 4].Text;
                    if (!string.IsNullOrWhiteSpace(studentCode))
                    {
                        students.Add(new Student
                        {
                            StudentCode = studentCode,
                            FirstName = firstName,
                            LastName = lastName,
                            CreatedBy = userId,
                            CreatedAt = DateTimeHelper.GetVietnamTime()
                        });
                    }
                }
            }
        }
        // Lấy danh sách StudentCode đã tồn tại
        var existingStudents = (await _repository.GetAllAsync()).ToDictionary(s => s.StudentCode);
        // Lấy ExamSessionSubjectId từ examSessionSubjectCore
        var examSessionSubject = await _examSessionSubjectRepository.GetQueryable().FirstOrDefaultAsync(x => x.ExamSessionSubjectCore == examSessionSubjectCore);
        if (examSessionSubject == null)
            throw new Exception($"Không tìm thấy ExamSessionSubject với core: {examSessionSubjectCore}");
        int? examRoomIdValue = examRoomId;
        int addedCount = 0;
        int studentExamSessionAdded = 0;
        foreach (var student in students)
        {
            Student dbStudent;
            if (!existingStudents.ContainsKey(student.StudentCode))
            {
                dbStudent = await _repository.AddAsync(student);
                addedCount++;
            }
            else
            {
                // Nếu đã tồn tại thì tăng version, cập nhật UpdatedBy, UpdatedAt
                dbStudent = existingStudents[student.StudentCode];
                dbStudent.Version += 1;
                dbStudent.UpdatedBy = userId;
                dbStudent.UpdatedAt = DateTimeHelper.GetVietnamTime();
                await _repository.UpdateAsync(dbStudent);
            }
            // Chỉ tạo mới nếu chưa có StudentExamSession trùng StudentId + ExamSessionSubjectId
            var exists = await _studentExamSessionRepository.GetQueryable()
                .AnyAsync(x => x.StudentId == dbStudent.StudentId && x.ExamSessionSubjectId == examSessionSubject.ExamSessionSubjectId);
            if (!exists)
            {
                var studentExamSession = new StudentExamSession
                {
                    StudentId = dbStudent.StudentId,
                    StudentCode = student.StudentCode,
                    ExamSessionSubjectId = examSessionSubject.ExamSessionSubjectId,
                    ExamRoomId = examRoomIdValue,
                    CreatedBy = userId,
                    CreatedAt = DateTimeHelper.GetVietnamTime(),
                    StudentAnswersString = "",
                    IsCompleted = false,
                    Score = 0
                };
                await _studentExamSessionRepository.AddAsync(studentExamSession);
                studentExamSessionAdded++;
            }
        }
        return new StudentImportResultDto { StudentsAdded = addedCount, StudentExamSessionsAdded = studentExamSessionAdded };
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

    #region GetExamFromRedisAsync
    private async Task<ShuffledExamPaperDto> GetExamFromRedisAsync(int shuffledExamPaperId)
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
                    var paperDto = System.Text.Json.JsonSerializer.Deserialize<ShuffledExamPaperDto>(cachedValue);
                    _logger.LogInformation("Đã lấy được đề thi từ Redis");
                    return paperDto;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi deserialize đề thi từ Redis");
                    await db.KeyDeleteAsync(cacheKey);
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi truy cập Redis để lấy đề thi");
            return null;
        }
    }
    #endregion

    #region GetExamFromDatabaseAsync
    private async Task<(ShuffledExamPaper ExamPaper, ShuffledExamPaperDto ExamPaperDto)> GetExamFromDatabaseAsync(int shuffledExamPaperId)
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
    #endregion

    #region StartExamAsync
    public async Task<ShuffledExamPaperDto> StartExamAsync(string studentCode, int studentExamSessionId)
    {
        _logger.LogInformation("Bắt đầu lấy đề thi cho sinh viên {StudentCode}, phiên thi {StudentExamSessionId}", 
            studentCode, studentExamSessionId);

        // 1. Kiểm tra StudentExamSession trong Redis cache trước
        var db = _redis.GetDatabase();
        string sessionCacheKey = $"student_exam_session:{studentCode}:{studentExamSessionId}";
        
        StudentExamSession? studentExamSession = null;
        int? shuffledExamPaperId = null;
        int examSessionSubjectId = 0;

        ShuffledExamPaper shuffledExamPaper = null;
        ShuffledExamPaperDto paperDto = null;

        try
        {
            // Thử lấy từ Redis cache trước
            var cachedSession = await db.StringGetAsync(sessionCacheKey);
            if (cachedSession.HasValue)
            {
                _logger.LogInformation("Tìm thấy StudentExamSession trong Redis cache cho sinh viên {StudentCode}", studentCode);
                studentExamSession = System.Text.Json.JsonSerializer.Deserialize<StudentExamSession>(cachedSession);
                shuffledExamPaperId = studentExamSession?.ShuffledExamPaperId;
                examSessionSubjectId = studentExamSession?.ExamSessionSubjectId ?? 0;
                
                
                // Kiểm tra nếu đã có ShuffledExamPaperId thì thử lấy từ Redis
                if (shuffledExamPaperId.HasValue)
                {
                    _logger.LogInformation("StudentExamSession đã có ShuffledExamPaperId: {ShuffledExamPaperId}, thử lấy từ Redis", shuffledExamPaperId.Value);
                    try
                    {
                        paperDto = await GetExamFromRedisAsync(shuffledExamPaperId.Value);
                        if (paperDto != null)
                        {
                            _logger.LogInformation("Đã lấy được đề thi từ Redis cho sinh viên {StudentCode}", studentCode);
                            return paperDto;
                        }
                        else
                        {
                            _logger.LogInformation("Không tìm thấy đề thi trong Redis, sẽ lấy từ database");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Lỗi khi lấy đề thi từ Redis, sẽ lấy từ database");
                    }
                }
                else
                {
                    _logger.LogInformation("StudentExamSession chưa có ShuffledExamPaperId, sẽ random đề thi");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi đọc StudentExamSession từ Redis cache, sẽ truy vấn database");
        }

        // 2. Nếu không có trong cache, truy vấn database
        if (studentExamSession == null)
        {
            _logger.LogInformation("Không tìm thấy StudentExamSession trong cache, truy vấn database");
            studentExamSession = await _studentExamSessionRepository.GetQueryable()
                .Include(x => x.ShuffledExamPaper)
                .FirstOrDefaultAsync(x => x.StudentCode == studentCode && x.StudentExamSessionId == studentExamSessionId);
            
            if (studentExamSession == null)
            {
                _logger.LogError("Không tìm thấy phiên thi của sinh viên {StudentCode}", studentCode);
                throw new Exception("Không tìm thấy phiên thi của sinh viên.");
            }

            // Cache StudentExamSession vào Redis
            try
            {
                var jsonSession = System.Text.Json.JsonSerializer.Serialize(studentExamSession);
                await db.StringSetAsync(sessionCacheKey, jsonSession, TimeSpan.FromHours(6));
                _logger.LogInformation("Đã cache StudentExamSession vào Redis cho sinh viên {StudentCode}", studentCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi cache StudentExamSession vào Redis");
            }

            shuffledExamPaperId = studentExamSession.ShuffledExamPaperId;
            examSessionSubjectId = studentExamSession.ExamSessionSubjectId;
            
            // Kiểm tra nếu đã có ShuffledExamPaperId từ database thì thử lấy từ Redis
            if (shuffledExamPaperId.HasValue)
            {
                _logger.LogInformation("StudentExamSession từ database đã có ShuffledExamPaperId: {ShuffledExamPaperId}, thử lấy từ Redis", shuffledExamPaperId.Value);
                try
                {
                    paperDto = await GetExamFromRedisAsync(shuffledExamPaperId.Value);
                    if (paperDto != null)
                    {
                        _logger.LogInformation("Đã lấy được đề thi từ Redis cho sinh viên {StudentCode}", studentCode);
                        return paperDto;
                    }
                    else
                    {
                        _logger.LogInformation("Không tìm thấy đề thi trong Redis, sẽ lấy từ database");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi lấy đề thi từ Redis, sẽ lấy từ database");
                }
            }
            else
            {
                _logger.LogInformation("StudentExamSession từ database chưa có ShuffledExamPaperId, sẽ random đề thi");
            }
        }
        
        // 3. Xử lý khi đã có ShuffledExamPaperId (từ cache hoặc database)
        if (shuffledExamPaperId.HasValue)
        {
            // Nếu chưa lấy được từ Redis ở bước trước, thử lấy từ database
            if (paperDto == null)
            {
                _logger.LogInformation("Lấy đề thi từ database cho ShuffledExamPaperId: {ShuffledExamPaperId}", shuffledExamPaperId.Value);
                var (examPaper, examPaperDto) = await GetExamFromDatabaseAsync(shuffledExamPaperId.Value);
                shuffledExamPaper = examPaper;
                paperDto = examPaperDto;

                // Cache lại vào Redis
                try
                {
                    string cacheKey = $"shuffled_exam_paper:{shuffledExamPaperId.Value}";
                    string answerKey = $"answer_key:{shuffledExamPaperId.Value}";
                    
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(paperDto);
                    var batch = db.CreateBatch();
                    
                    // Thực hiện cache đồng thời
                    var cacheTask = batch.StringSetAsync(cacheKey, jsonString, TimeSpan.FromHours(6));
                    var answerKeyTask = batch.StringSetAsync(answerKey, shuffledExamPaper.AnswerKey, TimeSpan.FromHours(6));
                    
                    batch.Execute();
                    await Task.WhenAll(cacheTask, answerKeyTask);
                    
                    _logger.LogInformation("Đã cache đề thi và answer key vào Redis");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi cache đề thi vào Redis");
                }
            }
        }
        else
        {
            _logger.LogInformation("Sinh viên {StudentCode} chưa được gán đề thi, đang chọn đề ngẫu nhiên", studentCode);
            
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
            shuffledExamPaper = availablePapers[random.Next(availablePapers.Count)];
            
            _logger.LogInformation("Đã chọn ngẫu nhiên đề thi {ShuffledExamPaperId} cho sinh viên {StudentCode}", 
                shuffledExamPaper.ShuffledExamPaperId, studentCode);
            
            // Cập nhật ShuffledExamPaperId vào StudentExamSession trong Redis
            try
            {
                // Lấy dữ liệu hiện tại từ Redis
                var existingSessionJson = await db.StringGetAsync(sessionCacheKey);
                if (existingSessionJson.HasValue)
                {
                    // Deserialize JSON hiện tại
                    var existingSession = System.Text.Json.JsonSerializer.Deserialize<StudentExamSession>(existingSessionJson);
                    if (existingSession != null)
                    {
                        // Cập nhật ShuffledExamPaperId
                        existingSession.ShuffledExamPaperId = shuffledExamPaper.ShuffledExamPaperId;
                        
                        // Serialize lại và lưu vào Redis
                        var updatedSessionJson = System.Text.Json.JsonSerializer.Serialize(existingSession);
                        await db.StringSetAsync(sessionCacheKey, updatedSessionJson, TimeSpan.FromHours(6));
                        
                        _logger.LogInformation("Đã cập nhật ShuffledExamPaperId {ShuffledExamPaperId} vào StudentExamSession trong Redis cho sinh viên {StudentCode}", 
                            shuffledExamPaper.ShuffledExamPaperId, studentCode);
                    }
                    else
                    {
                        _logger.LogWarning("Không thể deserialize StudentExamSession từ Redis cho sinh viên {StudentCode}", studentCode);
                    }
                }
                else
                {
                    _logger.LogWarning("Không tìm thấy StudentExamSession trong Redis cho sinh viên {StudentCode}", studentCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi cập nhật ShuffledExamPaperId vào StudentExamSession trong Redis cho sinh viên {StudentCode}", studentCode);
            }
            
            // Thử lấy đề từ Redis trước
            try
            {
                paperDto = await GetExamFromRedisAsync(shuffledExamPaper.ShuffledExamPaperId);
                if (paperDto == null)
                {
                    // Nếu không có trên Redis, lấy từ database
                    var (examPaper, examPaperDto) = await GetExamFromDatabaseAsync(shuffledExamPaper.ShuffledExamPaperId);
                    shuffledExamPaper = examPaper;
                    paperDto = examPaperDto;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể lấy đề thi từ Redis, sẽ lấy từ database");
                // Nếu Redis lỗi, lấy từ database
                var (examPaper, examPaperDto) = await GetExamFromDatabaseAsync(shuffledExamPaper.ShuffledExamPaperId);
                shuffledExamPaper = examPaper;
                paperDto = examPaperDto;
            }

            // Khởi tạo chuỗi đáp án rỗng
            var answerCount = shuffledExamPaper.AnswerKey.Split(';', StringSplitOptions.RemoveEmptyEntries).Length;
            var emptyAnswers = string.Join(";", Enumerable.Range(1, answerCount)
                .Select(i => $"({i},-)")) + ";";

            // Cập nhật thông tin vào database
            studentExamSession.StartTime = DateTimeHelper.GetVietnamTime();
            studentExamSession.ShuffledExamPaperId = shuffledExamPaper.ShuffledExamPaperId;
            studentExamSession.StudentAnswersString = emptyAnswers;
            await _studentExamSessionRepository.UpdateAsync(studentExamSession); //đợi dùng rabit 
            
            _logger.LogInformation("Đã cập nhật thông tin đề thi và chuỗi đáp án rỗng cho sinh viên trong database");
            
            // Cache vào Redis
            try
            {
                db = _redis.GetDatabase();
                string cacheKey = $"shuffled_exam_paper:{shuffledExamPaper.ShuffledExamPaperId}";
                string answerKey = $"answer_key:{shuffledExamPaper.ShuffledExamPaperId}";
                string studentAnswerKey = $"student_answers:{studentCode}:{shuffledExamPaper.ShuffledExamPaperId}";

                // Kiểm tra sự tồn tại của các key
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
                else
                {
                    _logger.LogInformation("Đề thi đã tồn tại trong Redis");
                }

                if (!answerKeyExists)
                {
                    _logger.LogInformation("Cache answer key mới vào Redis");
                    tasks.Add(batch.StringSetAsync(answerKey, shuffledExamPaper.AnswerKey, TimeSpan.FromHours(6)));
                }
                else
                {
                    _logger.LogInformation("Answer key đã tồn tại trong Redis");
                }

                if (!studentAnswerExists)
                {
                    _logger.LogInformation("Cache student answers mới vào Redis");
                    tasks.Add(batch.StringSetAsync(studentAnswerKey, emptyAnswers, TimeSpan.FromHours(6)));
                }
                else
                {
                    _logger.LogInformation("Student answers đã tồn tại trong Redis");
                }
                
                if (tasks.Any())
                {
                    batch.Execute();
                    await Task.WhenAll(tasks);
                    _logger.LogInformation("Đã hoàn thành cache dữ liệu mới vào Redis");
                }
                else
                {
                    _logger.LogInformation("Không có dữ liệu mới cần cache vào Redis");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cache dữ liệu vào Redis");
            }
        }

        _logger.LogInformation("Hoàn thành quá trình lấy đề thi cho sinh viên {StudentCode}, mã đề: {ShuffledExamPaperId}, số câu hỏi: {QuestionCount}", 
            studentCode,
            paperDto.ShuffledExamPaperId,
            paperDto.Details?.Count ?? 0);
            
        return paperDto;
    }
    #endregion
   




    #region GetStudentExamSessionsAsync
    public async Task<IEnumerable<StudentExamSessionDto>> GetStudentExamSessionsAsync(string studentCode)
    {
        var student = await _repository.GetQueryable().FirstOrDefaultAsync(x => x.StudentCode == studentCode);
        if (student == null) return Enumerable.Empty<StudentExamSessionDto>();
        var sessions = await _studentExamSessionRepository.GetQueryable()
            .Where(x => x.StudentId == student.StudentId && x.IsCompleted == false)
            .Include(x => x.ExamSessionSubject)
                .ThenInclude(x => x.Subject)
            .Include(x => x.ExamRoom)
            .ToListAsync();
        return sessions.Select(x => _mapper.Map<StudentExamSessionDto>(x));
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
    #region GetAndUpdateStudentAnswersAsync
    private async Task<(bool Success, string Message, Dictionary<int, string>? CurrentAnswers)> GetAndUpdateStudentAnswersAsync(
        string studentCode, int shuffledExamPaperId, List<SaveAnswerDto> saveAnswerDtos)
    {
        var db = _redis.GetDatabase();
        
        // Lấy student answers từ Redis
        string studentAnswerKey = $"student_answers:{studentCode}:{shuffledExamPaperId}";
        var studentAnswers = await db.StringGetAsync(studentAnswerKey);
        
        if (!studentAnswers.HasValue)
        {
            _logger.LogError("Không tìm thấy bài làm của sinh viên trong Redis");
            return (false, "Không tìm thấy bài làm của sinh viên", null);
        }

        // Cập nhật đáp án từ SubmitExamDto vào Redis
        var currentAnswers = studentAnswers.ToString().Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim('(', ')').Split(','))
            .ToDictionary(parts => int.Parse(parts[0]), parts => parts[1]);

        foreach (var answer in saveAnswerDtos)
        {
            if (currentAnswers.ContainsKey(answer.Index))
            {
                currentAnswers[answer.Index] = answer.Answer;
            }
        }

        // Tạo chuỗi đáp án mới
        var newAnswersString = string.Join(";", currentAnswers.Select(pair => $"({pair.Key},{pair.Value})")) + ";";

        // Lưu lại vào Redis
        await db.StringSetAsync(studentAnswerKey, newAnswersString, TimeSpan.FromHours(6));
        _logger.LogInformation("Đã cập nhật đáp án mới vào Redis: {NewAnswers}", newAnswersString);

        return (true, "Cập nhật đáp án thành công", currentAnswers);
    }
    #endregion

    #region ValidateStudentExamSessionAsync
    private async Task<(bool Success, string Message)> ValidateStudentExamSessionAsync(string studentCode, int shuffledExamPaperId)
    {
        var db = _redis.GetDatabase();
        
        // Tìm StudentExamSession từ cache với ShuffledExamPaperId cụ thể
        var sessionCacheKey = $"student_exam_session:{studentCode}:*";
        var keys = db.Multiplexer.GetServer(db.Multiplexer.GetEndPoints().First()).Keys(pattern: sessionCacheKey);
        
        foreach (var key in keys)
        {
            try
            {
                // Đọc dữ liệu dưới dạng string (vì được lưu bằng StringSetAsync)
                var sessionData = await db.StringGetAsync(key);
                if (sessionData.HasValue)
                {
                    var cachedStudentExamSession = System.Text.Json.JsonSerializer.Deserialize<StudentExamSession>(sessionData);
                    if (cachedStudentExamSession != null && cachedStudentExamSession.ShuffledExamPaperId == shuffledExamPaperId)
                    {
                        _logger.LogInformation($"[ValidateStudentExamSessionAsync] Truy vấn trạng thái từ cache StudentExamSession: {{Key}} = {{IsCompleted}}", key, cachedStudentExamSession.IsCompleted);
                        if (cachedStudentExamSession.IsCompleted)
                            return (false, "Bài thi đã được nộp trước đó");
                        return (true, "Validation thành công");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi đọc StudentExamSession từ Redis cache với key: {Key}", key);
                continue;
            }
        }

        _logger.LogInformation($"[ValidateStudentExamSessionAsync] Không có trạng thái trong cache, truy vấn DB");
        // Nếu không có trong cache, truy vấn DB
        var studentExamSession = await _studentExamSessionRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StudentCode == studentCode 
                && x.ShuffledExamPaperId == shuffledExamPaperId);

        if (studentExamSession == null)
            return (false, "Không tìm thấy phiên thi của sinh viên");

        if (studentExamSession.IsCompleted)
            return (false, "Bài thi đã được nộp trước đó");

        return (true, "Validation thành công");
    }
    #endregion

    #region GetAnswerKeyAsync
    private async Task<(bool Success, string Message, Dictionary<int, string>? CorrectAnswers)> GetAnswerKeyAsync(int shuffledExamPaperId)
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
    #endregion

    #region UpdateSingleAnswerAsync
    private async Task<(bool Success, string Message, string? NewAnswersString)> UpdateSingleAnswerAsync(
        string studentCode, int shuffledExamPaperId, int index, string answer)
    {
        var db = _redis.GetDatabase();
        string studentAnswerKey = $"student_answers:{studentCode}:{shuffledExamPaperId}";

        // Log thông tin
        _logger.LogInformation(
            "Đang lưu đáp án cho sinh viên. StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}, Index: {Index}, Answer: {Answer}",
            studentCode, shuffledExamPaperId, index, answer
        );

        // Lấy chuỗi đáp án hiện tại từ Redis
        var currentAnswers = await db.StringGetAsync(studentAnswerKey);
        
        if (!currentAnswers.HasValue)
        {
            _logger.LogError("Không tìm thấy chuỗi đáp án trong Redis với key: {Key}", studentAnswerKey);
            return (false, "Không tìm thấy bài thi của sinh viên", null);
        }

        string answersString = currentAnswers.ToString();

        // Tách chuỗi đáp án thành mảng
        var answerParts = answersString.Split(';', StringSplitOptions.RemoveEmptyEntries);
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

        // Lưu lại vào Redis với thời gian tồn tại 6 giờ
        await db.StringSetAsync(studentAnswerKey, newAnswersString, TimeSpan.FromHours(6));

        _logger.LogInformation("Đã lưu đáp án thành công vào Redis. Chuỗi đáp án mới: {NewAnswers}", newAnswersString);

        return (true, "Cập nhật đáp án thành công", newAnswersString);
    }
    #endregion

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
            var tasks = new[]
            {
                db.StringGetAsync(studentAnswerKey),
                db.StringGetAsync(answerKey),
                GetStudentExamSessionFromCacheAsync(db, sessionCacheKey, submitExamDto.ShuffledExamPaperId)
            };
            
            await Task.WhenAll(tasks);
            
            var studentAnswers = await tasks[0];
            var answerKeyValue = await tasks[1];
            var cachedSession = await tasks[2];
            
            // Validate và lấy dữ liệu song song
            var validationTask = ValidateStudentExamSessionOptimizedAsync(StudentCode, submitExamDto.ShuffledExamPaperId, cachedSession);
            var updateAnswersTask = UpdateStudentAnswersOptimizedAsync(studentAnswers, submitExamDto.SaveAnswerDtos, studentAnswerKey, db);
            
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
            
            var correctAnswerPairs = ParseAnswerKey(answerKeyValue.ToString());
            
            // Tính điểm tối ưu
            var (score, correctCount, totalQuestions) = CalculateScoreOptimized(currentAnswers, correctAnswerPairs);
            
            // Tạo message và gửi RabbitMQ
            var examSubmissionMessage = CreateExamSubmissionMessage(StudentCode, submitExamDto.ShuffledExamPaperId, score, correctCount, totalQuestions, currentAnswers);
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
            var (success, message, newAnswersString) = await UpdateSingleAnswerAsync(
                studentCode, shuffledExamPaperId, index, answer);
            
            if (!success)
            {
                return (false, message);
            }
            
            // Gửi message qua RabbitMQ
            var answerSavedMessage = _mapper.Map<StudentAnswerSavedMessage>(
                (studentCode, shuffledExamPaperId, index, answer, newAnswersString)
            );
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
            var tasks = new[]
            {
                db.StringGetAsync(studentAnswerKey),
                GetStudentExamSessionFromCacheAsync(db, sessionCacheKey, submitExamDto.ShuffledExamPaperId)
            };
            
            await Task.WhenAll(tasks);
            
            var studentAnswers = await tasks[0];
            var cachedSession = await tasks[1];
            
            // Validate và update answers song song
            var validationTask = ValidateStudentExamSessionOptimizedAsync(StudentCode, submitExamDto.ShuffledExamPaperId, cachedSession);
            var updateAnswersTask = UpdateStudentAnswersOptimizedAsync(studentAnswers, submitExamDto.SaveAnswerDtos, studentAnswerKey, db);
            
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
            var saveExamMessage = CreateSaveExamMessage(StudentCode, submitExamDto.ShuffledExamPaperId, currentAnswers);
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

    #region Helper methods for SubmitExamAsync optimization
    private async Task<StudentExamSession?> GetStudentExamSessionFromCacheAsync(IDatabase db, string sessionCacheKey, int shuffledExamPaperId)
    {
        try
        {
            var keys = db.Multiplexer.GetServer(db.Multiplexer.GetEndPoints().First()).Keys(pattern: sessionCacheKey);
            
            foreach (var key in keys)
            {
                var sessionData = await db.StringGetAsync(key);
                if (sessionData.HasValue)
                {
                    var cachedSession = System.Text.Json.JsonSerializer.Deserialize<StudentExamSession>(sessionData);
                    if (cachedSession?.ShuffledExamPaperId == shuffledExamPaperId)
                    {
                        return cachedSession;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi đọc StudentExamSession từ Redis cache");
        }
        
        return null;
    }
    
    private async Task<(bool Success, string Message)> ValidateStudentExamSessionOptimizedAsync(string studentCode, int shuffledExamPaperId, StudentExamSession? cachedSession)
    {
        if (cachedSession != null)
        {
            if (cachedSession.IsCompleted)
                return (false, "Bài thi đã được nộp trước đó");
            return (true, "Validation thành công");
        }
        
        // Fallback to database query
        var studentExamSession = await _studentExamSessionRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.StudentCode == studentCode && x.ShuffledExamPaperId == shuffledExamPaperId);
        
        if (studentExamSession == null)
            return (false, "Không tìm thấy phiên thi của sinh viên");
        
        if (studentExamSession.IsCompleted)
            return (false, "Bài thi đã được nộp trước đó");
        
        return (true, "Validation thành công");
    }
    
    private async Task<(bool Success, string Message, Dictionary<int, string>? CurrentAnswers)> UpdateStudentAnswersOptimizedAsync(
        RedisValue studentAnswers, List<SaveAnswerDto> saveAnswerDtos, string studentAnswerKey, IDatabase db)
    {
        if (!studentAnswers.HasValue)
        {
            _logger.LogError("Không tìm thấy bài làm của sinh viên trong Redis");
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
        
        // Cache với TTL
        await db.StringSetAsync(studentAnswerKey, newAnswersString, TimeSpan.FromHours(6));
        
        return (true, "Cập nhật đáp án thành công", currentAnswers);
    }
    
    private Dictionary<int, string> ParseAnswerKey(string answerKeyString)
    {
        return answerKeyString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim('(', ')').Split(','))
            .ToDictionary(parts => int.Parse(parts[0]), parts => parts[1]);
    }
    
    private Dictionary<int, string> ParseStudentAnswers(string answersString)
    {
        return answersString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(a => a.Trim('(', ')').Split(','))
            .ToDictionary(parts => int.Parse(parts[0]), parts => parts[1]);
    }
    
    private string CreateAnswersString(Dictionary<int, string> answers)
    {
        return string.Join(";", answers.Select(pair => $"({pair.Key},{pair.Value})")) + ";";
    }
    
    private (double Score, int CorrectCount, int TotalQuestions) CalculateScoreOptimized(
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
    
    private ExamSubmissionMessage CreateExamSubmissionMessage(string studentCode, int shuffledExamPaperId, 
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
    
    private ExamSubmissionMessage CreateSaveExamMessage(string studentCode, int shuffledExamPaperId, Dictionary<int, string> currentAnswers)
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
    #endregion

} 