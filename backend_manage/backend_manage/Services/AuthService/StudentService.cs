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

        // Gửi realtime trạng thái phòng thi cho tất cả session của sinh viên
        var studentExamSessions = await _studentExamSessionRepository.GetQueryable()
            .Where(x => x.StudentCode == studentCode1)
            .ToListAsync();

        foreach (var session in studentExamSessions)
        {
            if (session.ExamRoomId != null)
            {
                var examRoomId = session.ExamRoomId.Value;
                var examSessionSubjectId = session.ExamSessionSubjectId;
                var statusList = await GetStudentsByExamRoomAsync(examRoomId, examSessionSubjectId);
                Console.WriteLine($"[SignalR] Gửi RoomStatusUpdated tới room_{examRoomId} với {statusList.Count()} sinh viên.");
                await _hubContext.Clients.Group($"room_{examRoomId}")
                    .SendAsync("RoomStatusUpdated", statusList);
            }
        }
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

  

    public async Task<IEnumerable<Student>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Student?> GetByStudentCodeAsync(string studentCode)
    {
        var students = await _repository.GetAllAsync();
        return students.FirstOrDefault(s => s.StudentCode == studentCode);
    }

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

    public async Task<Student> UpdateAsync(string id, Student student)
    {
        return await _repository.UpdateAsync(student);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        return await _repository.DeleteAsync(id);
    }

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

    private async Task<(ShuffledExamPaper ExamPaper, ShuffledExamPaperDto ExamPaperDto)> GetExamFromDatabaseAsync(int shuffledExamPaperId)
    {
        _logger.LogInformation("Lấy đề thi từ database với ID: {ShuffledExamPaperId}", shuffledExamPaperId);
        
        var shuffledExamPaper = await _shuffledExamPaperRepository.GetQueryable()
            .Where(x => x.ShuffledExamPaperId == shuffledExamPaperId)
            .Include(x => x.ShuffledExamPaperDetails)
            .ThenInclude(d => d.OriginalExamPaperDetail)
            .Include(x => x.OriginalExamPaper)
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

    public async Task<ShuffledExamPaperDto> StartExamAsync(string studentCode, int studentExamSessionId)
    {
        _logger.LogInformation("Bắt đầu lấy đề thi cho sinh viên {StudentCode}, phiên thi {StudentExamSessionId}", 
            studentCode, studentExamSessionId);

        // 1. Kiểm tra StudentExamSession đã có mã đề chưa
        var studentExamSession = await _studentExamSessionRepository.GetQueryable()
            .Include(x => x.ShuffledExamPaper)
            .FirstOrDefaultAsync(x => x.StudentCode == studentCode && x.StudentExamSessionId == studentExamSessionId);
        
        if (studentExamSession == null)
        {
            _logger.LogError("Không tìm thấy phiên thi của sinh viên {StudentCode}", studentCode);
            throw new Exception("Không tìm thấy phiên thi của sinh viên.");
        }

        int examSessionSubjectId = studentExamSession.ExamSessionSubjectId;
        int? shuffledExamPaperId = studentExamSession.ShuffledExamPaperId;
        ShuffledExamPaper shuffledExamPaper = null;
        ShuffledExamPaperDto paperDto = null;

        // Kiểm tra Redis có hoạt động không
        bool isRedisAvailable = _redis?.IsRedisConnected(_logger) ?? false;
        if (!isRedisAvailable)
        {
            _logger.LogWarning("Redis không khả dụng, sẽ lấy đề thi từ database");
        }
        
        if (shuffledExamPaperId.HasValue)
        {
            // Nếu Redis hoạt động thì thử lấy từ Redis
            if (isRedisAvailable)
            {
                paperDto = await GetExamFromRedisAsync(shuffledExamPaperId.Value);
                if (paperDto != null)
                {
                    return paperDto;
                }
            }

            // Lấy từ database nếu Redis không khả dụng hoặc không lấy được từ Redis
            var (examPaper, examPaperDto) = await GetExamFromDatabaseAsync(shuffledExamPaperId.Value);
            shuffledExamPaper = examPaper;
            paperDto = examPaperDto;

            // Cache lại vào Redis nếu Redis khả dụng
            if (isRedisAvailable)
            {
                try
                {
                    var db = _redis.GetDatabase();
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
            
            // Kiểm tra đề đã random có trên Redis chưa
            if (isRedisAvailable)
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
            else
            {
                // Nếu Redis không khả dụng, lấy từ database
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
            await _studentExamSessionRepository.UpdateAsync(studentExamSession);
            
            _logger.LogInformation("Đã cập nhật thông tin đề thi và chuỗi đáp án rỗng cho sinh viên trong database");
            
            // Cache vào Redis nếu Redis khả dụng
            if (isRedisAvailable)
            {
                try
                {
                    var db = _redis.GetDatabase();
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
        }

        _logger.LogInformation("Hoàn thành quá trình lấy đề thi cho sinh viên {StudentCode}, mã đề: {ShuffledExamPaperId}, số câu hỏi: {QuestionCount}", 
            studentCode,
            paperDto.ShuffledExamPaperId,
            paperDto.Details?.Count ?? 0);
            
        return paperDto;
    }
   
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

    public async Task<(bool Success, string Message, double? Score)> SubmitExamAsync(string StudentCode,SubmitExamDto submitExamDto)
    {
        try
        {
            // Kiểm tra Redis có khả dụng không
            bool isRedisAvailable = _redis?.IsRedisConnected(_logger) ?? false;
            if (!isRedisAvailable)
            {
                _logger.LogWarning("Redis không khả dụng khi nộp bài thi");
                return (false, "Không thể kết nối đến Redis", null);
            }

            var db = _redis.GetDatabase();
            
            // Lấy answer key từ Redis
            string answerKey = $"answer_key:{submitExamDto.ShuffledExamPaperId}";
            var answerKeyValue = await db.StringGetAsync(answerKey);
            
            if (!answerKeyValue.HasValue)
            {
                _logger.LogError("Không tìm thấy đáp án trong Redis");
                return (false, "Không tìm thấy đáp án", null);
            }

            // Lấy student answers từ Redis
            string studentAnswerKey = $"student_answers:{StudentCode}:{submitExamDto.ShuffledExamPaperId}";
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

            foreach (var answer in submitExamDto.SaveAnswerDtos)
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

            // Tìm StudentExamSession
            var studentExamSession = await _studentExamSessionRepository.GetQueryable()
                .FirstOrDefaultAsync(x => x.StudentCode == StudentCode 
                    && x.ShuffledExamPaperId == submitExamDto.ShuffledExamPaperId);

            if (studentExamSession == null)
            {
                _logger.LogError("Không tìm thấy phiên thi của sinh viên trong database");
                return (false, "Không tìm thấy phiên thi của sinh viên", null);
            }

            if (studentExamSession.IsCompleted)
            {
                _logger.LogWarning("Sinh viên đã nộp bài thi này rồi");
                return (false, "Bài thi đã được nộp trước đó", null);
            }
            
            // Tính điểm
            string correctAnswers = answerKeyValue.ToString();

            var correctAnswerPairs = correctAnswers.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim('(', ')').Split(','))
                .ToDictionary(parts => int.Parse(parts[0]), parts => parts[1]);

            int correctCount = 0;
            int totalQuestions = correctAnswerPairs.Count;

            foreach (var pair in correctAnswerPairs)
            {
                if (currentAnswers.TryGetValue(pair.Key, out string studentAnswer) 
                    && studentAnswer == pair.Value)
                {
                    correctCount++;
                }
            }

            double score = (double)correctCount / totalQuestions * 10;

            // Map và gửi message qua RabbitMQ
            var examSubmissionMessage = _mapper.Map<ExamSubmissionMessage>((
                StudentCode: StudentCode,
                ShuffledExamPaperId: submitExamDto.ShuffledExamPaperId,
                Score: score,
                CorrectAnswers: correctCount,
                TotalQuestions: totalQuestions,
                EndTime: DateTimeHelper.GetVietnamTime(),
                StudentAnswersString: newAnswersString
            ));

            _rabbitMQService.PublishMessage("exam_submission_queue", examSubmissionMessage);

            // Xóa student answers khỏi Redis vì đã nộp bài
            // await db.KeyDeleteAsync(studentAnswerKey); tạm thời giữ lại để debug

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

    public async Task<(bool Success, string Message)> SaveStudentAnswerAsync(string studentCode, int shuffledExamPaperId, int index, string answer)
    {
        try
        {
            // Kiểm tra Redis có khả dụng không
            bool isRedisAvailable = _redis?.IsRedisConnected(_logger) ?? false;
            if (!isRedisAvailable)
            {
                _logger.LogWarning("Redis không khả dụng khi lưu đáp án của sinh viên");
                return (false, "Không thể kết nối đến Redis");
            }

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
                return (false, "Không tìm thấy bài thi của sinh viên");
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
            
            // Gửi message qua RabbitMQ
            var answerSavedMessage = _mapper.Map<StudentAnswerSavedMessage>(
                (studentCode, shuffledExamPaperId, index, answer, backend_manage.Hubs.DateTimeHelper.GetVietnamTime())
            );
            _rabbitMQService.PublishMessage("student_answer_saved_queue", answerSavedMessage);
            
            return (true, "Đã lưu đáp án thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu đáp án của sinh viên. StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}, Index: {Index}",
                studentCode, shuffledExamPaperId, index);
            return (false, $"Lỗi khi lưu đáp án: {ex.Message}");
        }
    }

 

} 