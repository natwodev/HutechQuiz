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
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

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
        ILogger<StudentService> logger)
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
        var db = _redis.GetDatabase();

        if (shuffledExamPaperId.HasValue)
        {
            // 1. Thử lấy từ Redis trước
            string cacheKey = $"shuffled_exam_paper:{shuffledExamPaperId.Value}";
            _logger.LogInformation("Đang tìm đề thi từ Redis với key: {CacheKey}", cacheKey);
            
            var cachedPaper = await db.StringGetAsync(cacheKey);
            
            if (!cachedPaper.IsNull)
            {
                _logger.LogInformation("Tìm thấy đề thi trong Redis cho sinh viên {StudentCode}", studentCode);
                // Lấy được từ Redis
                paperDto = System.Text.Json.JsonSerializer.Deserialize<ShuffledExamPaperDto>(cachedPaper);
                _logger.LogInformation("Đã deserialize thành công đề thi từ Redis, mã đề: {ShuffledExamPaperId}", 
                    paperDto.ShuffledExamPaperId);
            }
            else
            {
                _logger.LogWarning("Không tìm thấy đề thi trong Redis, đang lấy từ database cho sinh viên {StudentCode}", 
                    studentCode);
                // Không có trong Redis, lấy từ database và cache lại
                shuffledExamPaper = await _shuffledExamPaperRepository.GetQueryable()
                    .Where(x => x.ShuffledExamPaperId == shuffledExamPaperId.Value)
                    .Include(x => x.ShuffledExamPaperDetails)
                    .ThenInclude(d => d.OriginalExamPaperDetail)
                    .Include(x => x.OriginalExamPaper)
                    .FirstOrDefaultAsync();
                
                if (shuffledExamPaper == null)
                {
                    _logger.LogError("Không tìm thấy đề thi hoán vị {ShuffledExamPaperId} trong database", 
                        shuffledExamPaperId.Value);
                    throw new Exception("Không tìm thấy đề thi hoán vị.");
                }
                
                // Map sang DTO và cache vào Redis
                paperDto = _mapper.Map<ShuffledExamPaperDto>(shuffledExamPaper);
                _logger.LogInformation("Đang cache đề thi vào Redis, mã đề: {ShuffledExamPaperId}", 
                    shuffledExamPaper.ShuffledExamPaperId);
                
                await db.StringSetAsync(cacheKey, 
                    System.Text.Json.JsonSerializer.Serialize(paperDto),
                    TimeSpan.FromHours(6));
                
                _logger.LogInformation("Đã cache thành công đề thi vào Redis");
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
            
            // Gán mã đề cho sinh viên
            studentExamSession.ShuffledExamPaperId = shuffledExamPaper.ShuffledExamPaperId;
            await _studentExamSessionRepository.UpdateAsync(studentExamSession);
            
            _logger.LogInformation("Đã gán đề thi cho sinh viên trong database");
            
            // Lấy đề từ Redis hoặc database
            string cacheKey = $"shuffled_exam_paper:{shuffledExamPaper.ShuffledExamPaperId}";
            _logger.LogInformation("Đang tìm đề thi từ Redis với key: {CacheKey}", cacheKey);
            
            var cachedPaper = await db.StringGetAsync(cacheKey);
            
            if (!cachedPaper.IsNull)
            {
                _logger.LogInformation("Tìm thấy đề thi trong Redis");
                // Lấy được từ Redis
                paperDto = System.Text.Json.JsonSerializer.Deserialize<ShuffledExamPaperDto>(cachedPaper);
            }
            else
            {
                _logger.LogWarning("Không tìm thấy đề thi trong Redis, đang lấy từ database");
                // Không có trong Redis, lấy từ database và cache lại
                var paper = await _shuffledExamPaperRepository.GetQueryable()
                    .Where(x => x.ShuffledExamPaperId == shuffledExamPaper.ShuffledExamPaperId)
                    .Include(x => x.ShuffledExamPaperDetails)
                    .ThenInclude(d => d.OriginalExamPaperDetail)
                    .Include(x => x.OriginalExamPaper)
                    .FirstOrDefaultAsync();
                
                if (paper == null)
                {
                    _logger.LogError("Không tìm thấy đề thi hoán vị {ShuffledExamPaperId} trong database", 
                        shuffledExamPaper.ShuffledExamPaperId);
                    throw new Exception("Không tìm thấy đề thi hoán vị.");
                }
                
                // Map sang DTO và cache vào Redis
                paperDto = _mapper.Map<ShuffledExamPaperDto>(paper);
                _logger.LogInformation("Đang cache đề thi vào Redis");
                
                await db.StringSetAsync(cacheKey, 
                    System.Text.Json.JsonSerializer.Serialize(paperDto),
                    TimeSpan.FromHours(6));
                
                _logger.LogInformation("Đã cache thành công đề thi vào Redis");
            }
        }

        _logger.LogInformation("Hoàn thành quá trình lấy đề thi cho sinh viên {StudentCode}", studentCode);
        // 3. Trả đề về cho frontend
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


 

} 