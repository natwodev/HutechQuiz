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
        StudentValidationHelper validationHelper)
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
        
        student.IsLogin = true;
        student.LastLoggedIn = DateTimeHelper.GetVietnamTime();
        await _repository.UpdateAsync(student);

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

    #region GetAllAsync
    public async Task<IEnumerable<Student>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }
    #endregion

    #region GetByStudentCodeAsync
    public async Task<Student?> GetByStudentCodeAsync(string studentCode)
    {
        try
        {
            // Thử lấy từ Redis cache trước
            var cachedStudent = await _studentCacheHelper.GetStudentFromRedisAsync(studentCode);
            if (cachedStudent != null)
            {
                _logger.LogDebug("Đã lấy sinh viên {StudentCode} từ Redis cache", studentCode);
                return cachedStudent;
            }
            
            _logger.LogDebug("Không tìm thấy sinh viên {StudentCode} trong Redis cache, kiểm tra database", studentCode);
            
            // Fallback về database
            var students = await _repository.GetAllAsync();
            var dbStudent = students.FirstOrDefault(s => s.StudentCode == studentCode);
            
            if (dbStudent != null)
            {
                // Cache lại vào Redis
                await _studentCacheHelper.UpdateStudentInRedisAsync(dbStudent);
                _logger.LogDebug("Đã tìm thấy sinh viên {StudentCode} trong database và cache lại vào Redis", studentCode);
            }
            
            return dbStudent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy sinh viên {StudentCode} từ cache hoặc database", studentCode);
            
            // Fallback về database nếu có lỗi
            try
            {
                var students = await _repository.GetAllAsync();
                return students.FirstOrDefault(s => s.StudentCode == studentCode);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Lỗi khi truy vấn database cho sinh viên {StudentCode}", studentCode);
                return null;
            }
        }
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
                        paperDto = await _examPaperHelper.GetExamFromRedisAsync(shuffledExamPaperId.Value);
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
                    paperDto = await _examPaperHelper.GetExamFromRedisAsync(shuffledExamPaperId.Value);
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
                var (examPaper, examPaperDto) = await _examPaperHelper.GetExamFromDatabaseAsync(shuffledExamPaperId.Value);
                shuffledExamPaper = examPaper;
                paperDto = examPaperDto;

                // Cache lại vào Redis
                await _examPaperHelper.CacheExamPaperAsync(shuffledExamPaperId.Value, paperDto, shuffledExamPaper.AnswerKey);
            }
        }
        else
        {
            shuffledExamPaper = await _examPaperHelper.GetRandomExamPaperAsync(examSessionSubjectId);
            
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
                paperDto = await _examPaperHelper.GetExamFromRedisAsync(shuffledExamPaper.ShuffledExamPaperId);
                if (paperDto == null)
                {
                    // Nếu không có trên Redis, lấy từ database
                    var (examPaper, examPaperDto) = await _examPaperHelper.GetExamFromDatabaseAsync(shuffledExamPaper.ShuffledExamPaperId);
                    shuffledExamPaper = examPaper;
                    paperDto = examPaperDto;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể lấy đề thi từ Redis, sẽ lấy từ database");
                // Nếu Redis lỗi, lấy từ database
                var (examPaper, examPaperDto) = await _examPaperHelper.GetExamFromDatabaseAsync(shuffledExamPaper.ShuffledExamPaperId);
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
