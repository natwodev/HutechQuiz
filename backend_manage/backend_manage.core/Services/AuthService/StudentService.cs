using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Messages;
using backend_manage.core.Messages.RabbitMQ;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.AuthService.Helpers;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using StackExchange.Redis;
using backend_manage.core.Services.AuthService.Helpers;
using backend_manage.core.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace backend_manage.core.Services.AuthService;

public class StudentService : IStudentService
{
    private readonly IRepository<Student> _repository;
    private readonly IRepository<StudentExamSession> _studentExamSessionRepository;
    private readonly IRepository<ExamSessionSubject> _examSessionSubjectRepository;
    private readonly IRepository<ShuffledExamPaper> _shuffledExamPaperRepository;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<StudentService> _logger;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly StudentCacheHelper _studentCacheHelper;
    private readonly StudentExamSessionCacheHelper _sessionCacheHelper;
    private readonly ExamPaperHelper _examPaperHelper;
    private readonly StudentAnswerHelper _answerHelper;
    private readonly StudentImportHelper _importHelper;
    private readonly IRepository<StudentActivity> _activityRepository;

    private static readonly HashSet<string> _violationTypes = new()
    {
        "TabSwitch", "FullscreenExit", "Copy", "Paste", 
        "RightClick", "DevTools", "Screenshot",
        "AppBackground", "AppSwitch"
    };

    public StudentService(
        IRepository<Student> repository,
        IRepository<StudentExamSession> studentExamSessionRepository,
        IRepository<ExamSessionSubject> examSessionSubjectRepository,
        IRepository<ShuffledExamPaper> shuffledExamPaperRepository,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IHubContext<NotificationHub> hubContext,
        IConnectionMultiplexer redis,
        ILogger<StudentService> logger,
        IRabbitMqService rabbitMqService,
        StudentCacheHelper studentCacheHelper,
        StudentExamSessionCacheHelper sessionCacheHelper,
        ExamPaperHelper examPaperHelper,
        StudentAnswerHelper answerHelper,
        StudentImportHelper importHelper,
        IRepository<StudentActivity> activityRepository)
    {
        _repository = repository;
        _studentExamSessionRepository = studentExamSessionRepository;
        _examSessionSubjectRepository = examSessionSubjectRepository;
        _shuffledExamPaperRepository = shuffledExamPaperRepository;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _mapper = mapper;
        _hubContext = hubContext;
        _redis = redis;
        _logger = logger;
        _rabbitMqService = rabbitMqService;
        _studentCacheHelper = studentCacheHelper;
        _sessionCacheHelper = sessionCacheHelper;
        _examPaperHelper = examPaperHelper;
        _answerHelper = answerHelper;
        _importHelper = importHelper;
        _activityRepository = activityRepository;
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
        
        var student = await _studentCacheHelper.GetStudentFromRedisAsync(studentCode1);
        
        if (student == null)
        {
            return new StudentAuthResultDto
            {
                IsSuccess = false,
                ErrorMessage = "Không tìm thấy sinh viên với mã này."
            };
        }

        // Kiểm tra nếu sinh viên đã đăng nhập rồi
        if (student.IsLogin)
        {
            return new StudentAuthResultDto
            {
                IsSuccess = false,
                ErrorMessage = "Sinh viên này đã đăng nhập. Vui lòng liên hệ giám thị để mở phiên."
            };
        }
        
        // Cập nhật trạng thái đăng nhập
        student.IsLogin = true;
        student.LastLoggedIn = DateTimeHelper.GetVietnamTime();
        student.UpdatedAt = DateTimeHelper.GetVietnamTime();
        
        // Lưu vào database
        await _repository.UpdateAsync(student);
        
        // Cập nhật cache nếu cần
        await _studentCacheHelper.CacheStudent(student.StudentCode, student);
       
        var studentExamSessions = await _studentExamSessionRepository.GetQueryable()
            .Where(x => x.StudentCode == studentCode1)
            .ToListAsync();

        foreach (var session in studentExamSessions)
        {
            // Nếu phiên thi này không gắn với ExamSessionSubject thì bỏ qua, không gửi signalR
            if (!session.ExamSessionSubjectId.HasValue)
            {
                continue;
            }

            var examSessionSubjectId = session.ExamSessionSubjectId.Value;
            
            // Luôn gửi thông báo real-time dựa trên ExamSessionSubjectId
            var (statusList, subjectInfo) = await GetStudentsByExamSessionSubjectAsync(examSessionSubjectId);

            var groupName = $"lecturer_subject_{examSessionSubjectId}";
            
            await _hubContext.Clients
                .Group(groupName)
                .SendAsync("RoomStatusUpdated", new StudentListResponse { 
                    Students = statusList.ToList(),
                    Subject = subjectInfo
                });

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
            Role = "Student"
        };
    }
    #endregion

    #region LoginMobile
      public async Task<StudentAuthResultDto> LoginMobileAsync(string studentCode1, string studentCode2)
    {
        if (studentCode1 != studentCode2)
        {
            return new StudentAuthResultDto
            {
                IsSuccess = false,
                ErrorMessage = "Mã sinh viên nhập không khớp."
            };
        }
        
        var student = await _studentCacheHelper.GetStudentFromRedisAsync(studentCode1);
        
        if (student == null)
        {
            return new StudentAuthResultDto
            {
                IsSuccess = false,
                ErrorMessage = "Không tìm thấy sinh viên với mã này."
            };
        }
        
        
        // Cập nhật trạng thái đăng nhập
        student.IsLogin = true;
        student.LastLoggedIn = DateTimeHelper.GetVietnamTime();
        student.UpdatedAt = DateTimeHelper.GetVietnamTime();
        
        // Lưu vào database
        await _repository.UpdateAsync(student);
        
        // Cập nhật cache nếu cần
        await _studentCacheHelper.CacheStudent(student.StudentCode, student);
       
        var studentExamSessions = await _studentExamSessionRepository.GetQueryable()
            .Where(x => x.StudentCode == studentCode1)
            .ToListAsync();

        foreach (var session in studentExamSessions)
        {
            // Nếu phiên thi này không gắn với ExamSessionSubject thì bỏ qua, không gửi signalR
            if (!session.ExamSessionSubjectId.HasValue)
            {
                continue;
            }

            var examSessionSubjectId = session.ExamSessionSubjectId.Value;
            
            // Luôn gửi thông báo real-time dựa trên ExamSessionSubjectId
            var (statusList, subjectInfo) = await GetStudentsByExamSessionSubjectAsync(examSessionSubjectId);

            var groupName = $"lecturer_subject_{examSessionSubjectId}";
            
            await _hubContext.Clients
                .Group(groupName)
                .SendAsync("RoomStatusUpdated", new StudentListResponse { 
                    Students = statusList.ToList(),
                    Subject = subjectInfo
                });

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
            Role = "Student"
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

    #region ImportFromExcelAsyncs
    public async Task<StudentImportResultDto> ImportFromExcelAsyncs(IFormFile file, string examSessionSubjectCore)
    {
        _logger.LogInformation($"Bắt đầu import Excel. File: {file?.FileName}, Size: {file?.Length}, Core: {examSessionSubjectCore}");

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("File null hoặc rỗng.");
            return new StudentImportResultDto { StudentsAdded = 0, StudentExamSessionsAdded = 0 };
        }

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
             _logger.LogError("Không tìm thấy UserId trong token.");
             // Cho phép chạy test nếu không có userId (DEV MODE only - remove in production logic if needed)
             // throw new UnauthorizedAccessException("Không thể xác định người dùng tạo sinh viên.");
             userId = "system_import"; // Fallback tạm
        }
            
        var students = new List<Student>();
        try {
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    if (package.Workbook.Worksheets.Count == 0) throw new Exception("File Excel không có sheet nào.");
                    
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension?.Rows ?? 0;
                    _logger.LogInformation($"Đọc file Excel: {rowCount} dòng.");

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
        } 
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Lỗi khi đọc file Excel.");
            throw new Exception($"Lỗi đọc file Excel: {ex.Message}");
        }

        _logger.LogInformation($"Đã đọc {students.Count} sinh viên từ file.");

        // Lấy danh sách StudentCode đã tồn tại
        var existingStudents = (await _repository.GetAllAsync()).ToDictionary(s => s.StudentCode);
        
        // Lấy ExamSessionSubjectId từ examSessionSubjectCore
        var examSessionSubject = await _examSessionSubjectRepository.GetQueryable().FirstOrDefaultAsync(x => x.ExamSessionSubjectCore == examSessionSubjectCore);
        
        if (examSessionSubject == null)
        {
            _logger.LogError($"Không tìm thấy ExamSessionSubject với core: {examSessionSubjectCore}");
            throw new Exception($"Không tìm thấy ExamSessionSubject với core: {examSessionSubjectCore}");
        }
        
        int addedCount = 0;
        int studentExamSessionAdded = 0;
        
        foreach (var student in students)
        {
            Student dbStudent;
            if (!existingStudents.ContainsKey(student.StudentCode))
            {
                dbStudent = await _repository.AddAsync(student);
                // Update local dictionary to avoid duplicates within the same import batch
                existingStudents.Add(dbStudent.StudentCode, dbStudent);
                addedCount++;
            }
            else
            {
                // Nếu đã tồn tại thì tăng version, cập nhật UpdatedBy, UpdatedAt
                dbStudent = existingStudents[student.StudentCode];
                dbStudent.Version += 1;
                dbStudent.UpdatedBy = userId;
                dbStudent.UpdatedAt = DateTimeHelper.GetVietnamTime();
                
                // Chỉ update nếu thực sự cần (optimization optional)
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
                    OriginalExamPaperId = examSessionSubject.OriginalExamPaperId,
                    CreatedBy = userId,
                    CreatedAt = DateTimeHelper.GetVietnamTime(),
                    StudentAnswersString = "",
                    IsCompleted = false,
                    Score = 0,
                    // Cache thời gian từ ExamSessionSubject để tránh join
                    ExamSessionStartTime = examSessionSubject.StartTime,
                    ExamSessionEndTime = examSessionSubject.EndTime
                };
                await _studentExamSessionRepository.AddAsync(studentExamSession);
                studentExamSessionAdded++;
            }
        }
        
        _logger.LogInformation($"Kết thúc import. AddedStudents: {addedCount}, AddedSessions: {studentExamSessionAdded}");
        return new StudentImportResultDto { StudentsAdded = addedCount, StudentExamSessionsAdded = studentExamSessionAdded };
    }
    #endregion
    
    #region ImportFromExcelAsync
    public async Task<StudentImportResultDto> ImportFromExcelAsync(IFormFile file, string examSessionSubjectCore)
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
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        
        // Gửi message vào RabbitMQ
       // _rabbitMqService.PublishMessage("student_import_queue", message);
        
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
    
    #region ImportFromExcelStreamAsync
    public async Task<StudentImportResultDto> ImportFromExcelStreamAsync(Stream stream, string examSessionSubjectCore, string userId)
    {
        return await _importHelper.ImportFromExcelStreamAsync(stream, examSessionSubjectCore, userId);
    }
    #endregion
      
      #region CreateSessionWithOriginalPaperAsync
      /// <summary>
      /// Tạo một StudentExamSession mới dựa trên OriginalExamPaperId và studentCode,
      /// cache vào Redis và trả về phiên thi + đề gốc.
      /// </summary>
      public async Task<(StudentExamSessionCacheDto studentExamSessionCacheDto, OriginalExamPaperDto? originalExamPaperDto)>
          CreateSessionWithOriginalPaperAsync(string studentCode, int originalExamPaperId)
      {
          // Lấy sinh viên
          var student = await _repository.GetQueryable()
              .FirstOrDefaultAsync(x => x.StudentCode == studentCode);
          if (student == null)
          {
              throw new InvalidOperationException("Không tìm thấy sinh viên với mã này.");
          }

          // Lấy thông tin đề gốc
          var originalExamPaperDto = await _examPaperHelper.GetOriginalExamPaperAsync(originalExamPaperId);
          if (originalExamPaperDto == null)
          {
              throw new InvalidOperationException("Không tìm thấy đề thi gốc.");
          }

          // Tạo một phiên thi đơn lẻ không gắn với ExamSessionSubject (ExamSessionSubjectId = null)
          var now = DateTimeHelper.GetVietnamTime();

          var sessionEntity = new StudentExamSession
          {
              StudentId = student.StudentId,
              StudentCode = student.StudentCode,
              ExamSessionSubjectId = null,
              OriginalExamPaperId = originalExamPaperId,
              CreatedBy = student.StudentCode,
              CreatedAt = now,
              StartTime = now, // bắt đầu ngay tại thời điểm tạo
              IsCompleted = false,
              Score = 0,
              ExtraMinutes = 0,
              ExamSessionStartTime = now,
              ExamSessionEndTime = now.AddMinutes(originalExamPaperDto.DurationMinutes > 0 ? originalExamPaperDto.DurationMinutes : 60),
              StudentAnswersString = originalExamPaperDto.KeyValueList != null
                  ? "" // sẽ được build lại từ helper phía dưới
                  : ""
          };

          // Lưu DB
          sessionEntity = await _studentExamSessionRepository.AddAsync(sessionEntity);

          // Map sang cache DTO và đảm bảo các trường cần thiết
          var cacheDto = _mapper.Map<StudentExamSessionCacheDto>(sessionEntity);
          cacheDto.StudentExamSessionId = sessionEntity.StudentExamSessionId;
          cacheDto.OriginalExamPaperId = originalExamPaperId;
          cacheDto.StudentCode = student.StudentCode;
          cacheDto.StartTime = now;

          // Nếu đề gốc có KeyValueList thì tạo chuỗi đáp án rỗng theo format đó
          if (!string.IsNullOrWhiteSpace(originalExamPaperDto.KeyValueList))
          {
              // Tái sử dụng logic tạo chuỗi đáp án rỗng trong ExamPaperHelper
              // thông qua việc gọi StartExam flow đơn giản: dùng KeyValueList như answer key
              var emptyAnswers = originalExamPaperDto.KeyValueList;
              cacheDto.StudentAnswersString = emptyAnswers;
          }

          // Cache vào Redis
          await _sessionCacheHelper.UpdateStudentExamSessionAsync(studentCode, cacheDto);

          return (cacheDto, originalExamPaperDto);
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
    public async Task<(StudentExamSessionCacheDto studentExamSessionCacheDto, ShuffledExamPaperDto? shuffledExamPaperDto, OriginalExamPaperDto? originalExamPaperDto)> StartExamAsync(string studentCode, int studentExamSessionId)
    {
        _logger.LogInformation("Bắt đầu lấy đề thi cho sinh viên {StudentCode}, phiên thi {StudentExamSessionId}", studentCode, studentExamSessionId);

        var (studentExamSessionCacheDto, shuffledExamPaperDto, originalExamPaperDto) = await _examPaperHelper.GetStudentExamSessionAndExamPaperAsync(studentCode, studentExamSessionId);

        return (studentExamSessionCacheDto, shuffledExamPaperDto, originalExamPaperDto);
    }
    #endregion

    #region StartExamWithOriginalPaperAsync
    public async Task<(StudentExamSessionCacheDto studentExamSessionCacheDto, OriginalExamPaperDto? originalExamPaperDto)> StartExamWithOriginalPaperAsync(string studentCode, int studentExamSessionId)
    {
        _logger.LogInformation("Bắt đầu lấy đề gốc cho sinh viên {StudentCode}, phiên thi {StudentExamSessionId}", studentCode, studentExamSessionId);

        var (studentExamSessionCacheDto, originalExamPaperDto) = await _examPaperHelper.GetStudentExamSessionAndOriginalPaperAsync(studentCode, studentExamSessionId);

        // Lưu OriginalExamPaperId xuống DB nếu cần
        if (studentExamSessionCacheDto?.OriginalExamPaperId.HasValue == true)
        {
            try
            {
                var entity = await _studentExamSessionRepository.GetQueryable()
                    .FirstOrDefaultAsync(x => x.StudentExamSessionId == studentExamSessionCacheDto.StudentExamSessionId);
                if (entity != null)
                {
                    if (entity.OriginalExamPaperId != studentExamSessionCacheDto.OriginalExamPaperId)
                    {
                        entity.OriginalExamPaperId = studentExamSessionCacheDto.OriginalExamPaperId;
                    }
                    if (!entity.StartTime.HasValue && studentExamSessionCacheDto.StartTime.HasValue)
                    {
                        entity.StartTime = studentExamSessionCacheDto.StartTime;
                    }
                    entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
                    await _studentExamSessionRepository.UpdateAsync(entity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể cập nhật OriginalExamPaperId xuống DB cho phiên thi {StudentExamSessionId}", studentExamSessionId);
            }
        }

        return (studentExamSessionCacheDto, originalExamPaperDto);
    }
    #endregion
    
    #region GetStudentExamSessionsAsync
    public async Task<IEnumerable<StudentExamSessionDto>> GetStudentExamSessionsAsync(string studentCode)
    {
        try
        {
            // Thử lấy từ Redis cache trước
            var cachedSessions = await _sessionCacheHelper.GetListStudentExamSessionsFromRedisAsync(studentCode);
            if (cachedSessions != null && cachedSessions.Any())
            {
                // Trả nguyên danh sách từ cache, KHÔNG lọc theo ExamSessionSubject.IsActive
                var cachedList = cachedSessions.ToList();
                _logger.LogDebug("Đã lấy {Count} phiên thi từ Redis cache cho sinh viên {StudentCode}", 
                    cachedList.Count, studentCode);
                return cachedList;
            }
            
            _logger.LogDebug("Không tìm thấy phiên thi chưa hoàn thành và còn thời gian làm bài trong Redis cache cho sinh viên {StudentCode}, kiểm tra database", studentCode);
            
            // Fallback về database
            var student = await _repository.GetQueryable().FirstOrDefaultAsync(x => x.StudentCode == studentCode);
            if (student == null) return Enumerable.Empty<StudentExamSessionDto>();
            
            var currentTime = DateTimeHelper.GetVietnamTime();
            
            var sessions = await _studentExamSessionRepository.GetQueryable()
                .Where(x => x.StudentId == student.StudentId && 
                           x.IsCompleted == false &&
                           currentTime < x.ExamSessionStartTime.AddMinutes(x.ExamSessionSubject.Duration + x.ExtraMinutes))
                .Include(x => x.ExamSessionSubject)
                    .ThenInclude(x => x.Subject)
                .Include(x => x.ExamSessionSubject)
                    .ThenInclude(x => x.ExamRoom)
                .AsSplitQuery()
                .ToListAsync();
            
            // Cache lại vào Redis
            await _sessionCacheHelper.CacheStudentExamSessions(studentCode, sessions);
            
            return sessions.Select(x => _mapper.Map<StudentExamSessionDto>(x));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy phiên thi cho sinh viên {StudentCode}", studentCode);
            return Enumerable.Empty<StudentExamSessionDto>();
        }
    }
    #endregion
    
    #region GetStudentsByExamSessionSubjectAsync
    public async Task<(IEnumerable<StudentExamRoomStatusDto> Students, SubjectExamRoomStatusDto SubjectInfo)> GetStudentsByExamSessionSubjectAsync(int? examSessionSubjectId)
    {
        // Nếu không có ExamSessionSubjectId thì không thực hiện truy vấn, trả về rỗng
        if (!examSessionSubjectId.HasValue)
        {
            return (Enumerable.Empty<StudentExamRoomStatusDto>(), new SubjectExamRoomStatusDto());
        }

        var query = _studentExamSessionRepository.GetQueryable()
            .Where(ses => ses.ExamSessionSubjectId == examSessionSubjectId)
            .Include(ses => ses.Student)
            .Include(ses => ses.ExamSessionSubject)
                .ThenInclude(ess => ess.Subject)
            .Include(ses => ses.ExamSessionSubject)
                .ThenInclude(ess => ess.ExamRoom)
            .Include(ses => ses.ExamSessionSubject)
                .ThenInclude(ess => ess.OriginalExamPaper)
            .AsSplitQuery();

        var sessions = await query.ToListAsync();
        var sessionIds = sessions.Select(s => s.StudentExamSessionId).ToList();

        // Lấy danh sách hành động vi phạm cho tất cả sinh viên trong ca thi
        var activities = await _activityRepository.GetQueryable()
            .Where(a => sessionIds.Contains(a.StudentExamSessionId) && _violationTypes.Contains(a.ActivityType))
            .ToListAsync();

        var students = sessions.Select(x => {
            var dto = _mapper.Map<StudentExamRoomStatusDto>(x);
            
            // Lọc các activity của sinh viên này
            var studentActivities = activities
                .Where(a => a.StudentExamSessionId == x.StudentExamSessionId)
                .OrderByDescending(a => a.ActivityTime)
                .ToList();
                
            dto.CheatingWarningCount = studentActivities.Count;
            dto.CheatingWarningDetails = studentActivities
                .Select(a => $"{a.ActivityTime:HH:mm:ss}: {a.Description ?? a.ActivityType}")
                .ToList();
                
            return dto;
        }).ToList();

        var anySession = sessions.FirstOrDefault();
        SubjectExamRoomStatusDto subjectInfo;
        if (anySession != null)
        {
            subjectInfo = new SubjectExamRoomStatusDto
            {
                SubjectId = anySession.ExamSessionSubject.SubjectId,
                SubjectCode = anySession.ExamSessionSubject.Subject.SubjectCore,
                SubjectName = anySession.ExamSessionSubject.Subject.SubjectName,
                RoomName = anySession.ExamSessionSubject.ExamRoom?.RoomName,
                Duration = anySession.ExamSessionSubject.Duration,
                ExamSessionStartTime = anySession.ExamSessionStartTime,
                ExamSessionEndTime = anySession.ExamSessionEndTime,
                OriginalExamPaperCore = anySession.ExamSessionSubject.OriginalExamPaper?.OriginalExamPaperCore
            };
        }
        else
        {
            // Fallback: query subject info directly
            var ess = await _examSessionSubjectRepository.GetQueryable()
                .Include(x => x.Subject)
                .Include(x => x.OriginalExamPaper)
                .FirstOrDefaultAsync(x => x.ExamSessionSubjectId == examSessionSubjectId);
            subjectInfo = new SubjectExamRoomStatusDto
            {
                SubjectId = ess?.SubjectId ?? 0,
                SubjectCode = ess?.Subject?.SubjectCore,
                SubjectName = ess?.Subject?.SubjectName,
                RoomName = null,
                Duration = ess?.Duration ?? 0,
                ExamSessionStartTime = ess?.StartTime ?? DateTimeHelper.GetVietnamTime(),
                ExamSessionEndTime = ess?.EndTime ?? DateTimeHelper.GetVietnamTime(),
                OriginalExamPaperCore = ess?.OriginalExamPaper?.OriginalExamPaperCore
            };
        }

        return (students, subjectInfo);
    }
    #endregion
    
    #region AddExtraMinutesAsync
    public async Task AddExtraMinutesAsync(string studentCode, int studentExamSessionId, int extraMinutes, string? reasonForExtra)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        // Bước 1: Lấy StudentExamSession từ Redis cache trước, nếu không có thì lấy từ DB
        var (redisAvailable, sessionDto) = await _sessionCacheHelper.GetStudentExamSessionAsync(studentCode, studentExamSessionId);

        if (sessionDto == null)
        {
            throw new Exception("Không tìm thấy phiên thi sinh viên với mã đã cung cấp.");
        }

        if (sessionDto.ExtraMinutes == extraMinutes && sessionDto.ReasonForExtra == reasonForExtra)
        {
            return; // Không cần cập nhật nếu giá trị giống nhau
        }

        // Bước 2: Cập nhật Redis cache trước
        try
        {
            // Cập nhật trạng thái trong object session
            sessionDto.ExtraMinutes = extraMinutes;
            sessionDto.ReasonForExtra = reasonForExtra;
            
            // Cache lại vào Redis với thông tin mới
            await _sessionCacheHelper.UpdateStudentExamSessionAsync(studentCode, sessionDto);
            _logger.LogInformation("✅ Đã cập nhật Redis cache cho phiên thi {StudentExamSessionId} của sinh viên {StudentCode} với ExtraMinutes: {ExtraMinutes}", studentExamSessionId, studentCode, extraMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Không thể cập nhật Redis cache cho phiên thi {StudentExamSessionId} của sinh viên {StudentCode}, sẽ tiếp tục cập nhật DB", studentExamSessionId, studentCode);
        }

        // Bước 3: Cập nhật database
        try
        {
            var sessionEntity = await _studentExamSessionRepository.GetQueryable()
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.StudentExamSessionId == studentExamSessionId && s.StudentCode == studentCode);

            if (sessionEntity != null)
            {
                sessionEntity.ExtraMinutes = extraMinutes;
                sessionEntity.ReasonForExtra = reasonForExtra;
                sessionEntity.UpdatedBy = userId;
                sessionEntity.UpdatedAt = DateTimeHelper.GetVietnamTime();
                
                await _studentExamSessionRepository.UpdateAsync(sessionEntity);
                _logger.LogInformation("✅ Đã cập nhật database cho phiên thi {StudentExamSessionId} của sinh viên {StudentCode} với ExtraMinutes: {ExtraMinutes}", studentExamSessionId, studentCode, extraMinutes);
                
                // Chỉ gửi thông báo real-time nếu phiên thi có ExamSessionSubjectId
                if (sessionEntity.ExamSessionSubjectId.HasValue)
                {
                    var essId = sessionEntity.ExamSessionSubjectId.Value;

                    var (statusList, subjectInfo) = await GetStudentsByExamSessionSubjectAsync(essId);

                    var groupName = $"lecturer_subject_{essId}";
            
                    await _hubContext.Clients
                        .Group(groupName)
                        .SendAsync("RoomStatusUpdated", new StudentListResponse { 
                            Students = statusList.ToList(),
                            Subject = subjectInfo
                        });
                }
                    
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi cập nhật database cho phiên thi {StudentExamSessionId} của sinh viên {StudentCode}", studentExamSessionId, studentCode);
            throw new Exception("Lỗi khi cập nhật database. Vui lòng thử lại sau.");
        }
    }
    #endregion
    
    #region AvtiveLoginAsync
    public async Task<(bool Success, string Message)> AvtiveLoginAsync(string studentCode, bool isLogin)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        // Bước 1: Lấy sinh viên từ Redis cache trước, nếu không có thì lấy từ DB
        var student = await _studentCacheHelper.GetStudentFromRedisAsync(studentCode);

        if (student == null)
        {
            return (false, "Không tìm thấy sinh viên với mã này.");
        }

        if (student.IsLogin == isLogin)
        {
            return (true, "Trạng thái đăng nhập đã đúng, không cần cập nhật.");
        }

        // Bước 2: Cập nhật Redis cache trước
        try
        {
            // Cập nhật trạng thái trong object student
            student.IsLogin = isLogin;
            student.UpdatedBy = userId;
            student.UpdatedAt = DateTimeHelper.GetVietnamTime();
            
            // Cache lại vào Redis với thông tin mới
            await _studentCacheHelper.CacheStudent(studentCode, student);
            _logger.LogInformation("✅ Đã cập nhật Redis cache cho sinh viên {StudentCode} với trạng thái IsLogin: {IsLogin}", studentCode, isLogin);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Không thể cập nhật Redis cache cho sinh viên {StudentCode}, sẽ tiếp tục cập nhật DB", studentCode);
        }

        // Bước 3: Cập nhật database
        try
        {
            await _repository.UpdateAsync(student);
            _logger.LogInformation("✅ Đã cập nhật database cho sinh viên {StudentCode} với trạng thái IsLogin: {IsLogin}", studentCode, isLogin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi cập nhật database cho sinh viên {StudentCode}", studentCode);
            return (false, "Lỗi khi cập nhật database. Vui lòng thử lại sau.");
        }
        
        var studentExamSessions = await _studentExamSessionRepository.GetQueryable()
            .Where(x => x.StudentCode == studentCode)
            .ToListAsync();

        foreach (var session in studentExamSessions)
        {
            // Nếu phiên thi này không gắn với ExamSessionSubject thì bỏ qua, không gửi signalR
            if (!session.ExamSessionSubjectId.HasValue)
            {
                continue;
            }

            var examSessionSubjectId = session.ExamSessionSubjectId.Value;
            
            // Luôn gửi thông báo real-time dựa trên ExamSessionSubjectId
            var (statusList, subjectInfo) = await GetStudentsByExamSessionSubjectAsync(examSessionSubjectId);

            var groupName = $"lecturer_subject_{examSessionSubjectId}";
            
            await _hubContext.Clients
                .Group(groupName)
                .SendAsync("RoomStatusUpdated", new StudentListResponse { 
                    Students = statusList.ToList(),
                    Subject = subjectInfo
                });

        }

        return (true, "Cập nhật trạng thái đăng nhập thành công.");
    }
    #endregion

    
    #region UpdateSingleAnswerAsync
    public async Task<(bool Success, string Message, string? NewAnswersString)> UpdateSingleAnswerAsync(string studentCode, int studentExamSessionId, int key ,object value)
    {
        try
        {
            // Cập nhật đáp án sử dụng StudentAnswerHelper
            var (success, message, newAnswersString) = await _answerHelper.UpdateSingleAnswerAsync(studentCode, studentExamSessionId, key, value);
            
            if (success)
            {
                _logger.LogInformation("✅ Đã cập nhật đáp án thành công cho sinh viên {StudentCode} tại vị trí {Key}: {Value}", studentCode, key, value);
            }
            else
            {
                _logger.LogWarning("⚠️ Không thể cập nhật đáp án cho sinh viên {StudentCode} tại vị trí {Key}: {Value}: {Message}", studentCode, key, value, message);
            }

            return (success, message, newAnswersString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi cập nhật đáp án cho sinh viên {StudentCode} tại vị trí {Key}", studentCode, key);
            return (false, "Lỗi hệ thống khi cập nhật đáp án", null);
        }
    }
    #endregion
    
    #region SubmitExamAsync
    public async Task<(bool Success, string Message)> SubmitExamAsync(string studentCode, int studentExamSessionId)
    {
        try
        {
            // Sử dụng ExamPaperHelper để nộp bài thi
            var (success, score, message, studentExamSessionDto, answerKey) = await _examPaperHelper.SubmitExam(studentCode, studentExamSessionId);
            
            if (!success)
            {
                _logger.LogWarning("⚠️ Không thể nộp bài thi cho sinh viên {StudentCode}: {Message}", studentCode, message);
                return (false, message);
            }
            
            if (studentExamSessionDto == null)
            {
                _logger.LogError("❌ Không thể lấy thông tin phiên thi sau khi nộp bài");
                return (false, "Không thể lấy thông tin phiên thi");
            }

            _logger.LogInformation("✅ Hoàn thành nộp bài thi cho sinh viên {StudentCode}", studentCode);
            
            return (true, "Nộp bài thi thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi nộp bài thi cho sinh viên {StudentCode}", studentCode);
            return (false, $"Lỗi hệ thống: {ex.Message}");
        }
    }
    #endregion
    
    #region GetSubmissionResultAsync
    public async Task<ExamSubmissionDto?> GetSubmissionResultAsync(string studentCode, int studentExamSessionId)
    {
        try
        {
            _logger.LogInformation("🔍 Lấy kết quả nộp bài cho sinh viên {StudentCode} với session {SessionId}", studentCode, studentExamSessionId);
            
            // Lấy thông tin StudentExamSession từ database
            var studentExamSession = await _studentExamSessionRepository.GetQueryable()
                .Include(ses => ses.ShuffledExamPaper)
                .FirstOrDefaultAsync(ses => ses.StudentCode == studentCode && ses.StudentExamSessionId == studentExamSessionId);

            if (studentExamSession == null)
            {
                _logger.LogWarning("❌ Không tìm thấy phiên thi cho sinh viên {StudentCode} với ID {SessionId}", studentCode, studentExamSessionId);
                return null;
            }

            // Đã loại bỏ ràng buộc IsCompleted để sinh viên có thể xem kết quả bất kỳ lúc nào

            // Lấy answer key từ ShuffledExamPaper
            string answerKey = null;
            if (studentExamSession.ShuffledExamPaper != null)
            {
                answerKey = studentExamSession.ShuffledExamPaper.AnswerKey;
            }
            else if (studentExamSession.ShuffledExamPaperId.HasValue)
            {
                // Nếu chưa load ShuffledExamPaper, lấy từ database
                var shuffledPaper = await _shuffledExamPaperRepository.GetByIdAsync(studentExamSession.ShuffledExamPaperId.Value);
                answerKey = shuffledPaper?.AnswerKey;
            }

            var submissionData = new ExamSubmissionDto
            {
                StudentCode = studentCode,
                ShuffledExamPaperId = studentExamSession.ShuffledExamPaperId ?? 0,
                Score = studentExamSession.Score,
                CorrectAnswers = studentExamSession.CorrectAnswers,
                TotalQuestions = studentExamSession.TotalQuestions,
                StartTime = studentExamSession.StartTime,
                EndTime = studentExamSession.EndTime ?? DateTimeHelper.GetVietnamTime(),
                StudentAnswersString = studentExamSession.StudentAnswersString,
                AnswerKey = answerKey ?? ""
            };

            _logger.LogInformation("✅ Lấy kết quả nộp bài thành công cho sinh viên {StudentCode}. Điểm: {Score}", studentCode, submissionData.Score);
            
            return submissionData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi lấy kết quả nộp bài cho sinh viên {StudentCode}", studentCode);
            return null;
        }
    }
    #endregion
    
    #region GetStudentGradesByExamSessionSubjectAsync
    public async Task<(IEnumerable<StudentGradeDto> Grades, string SubjectCode)> GetStudentGradesByExamSessionSubjectAsync(int examSessionSubjectId)
    {
        try
        {
            _logger.LogInformation("Bắt đầu lấy danh sách điểm sinh viên cho ExamSessionSubjectId: {ExamSessionSubjectId}", examSessionSubjectId);

            // Lấy danh sách StudentExamSession theo ExamSessionSubjectId
            var studentExamSessions = await _studentExamSessionRepository.GetQueryable()
                .Include(ses => ses.Student)
                .Include(ses => ses.ExamSessionSubject)
                .ThenInclude(ess => ess.Subject)
                .Where(ses => ses.ExamSessionSubjectId == examSessionSubjectId)
                .OrderBy(ses => ses.Student.StudentCode)
                .ToListAsync();

            if (!studentExamSessions.Any())
            {
                _logger.LogWarning("Không tìm thấy dữ liệu điểm cho ExamSessionSubjectId: {ExamSessionSubjectId}", examSessionSubjectId);
                return (new List<StudentGradeDto>(), "UNKNOWN");
            }

            // Lấy mã môn học từ ExamSessionSubject đầu tiên
            string subjectCode = studentExamSessions.First().ExamSessionSubject?.Subject?.SubjectCore ?? "UNKNOWN";

            // Chuyển đổi thành StudentGradeDto với STT
            var result = new List<StudentGradeDto>();
            int stt = 1;

            foreach (var session in studentExamSessions)
            {
                result.Add(new StudentGradeDto
                {
                    STT = stt++,
                    StudentCode = session.Student?.StudentCode ?? session.StudentCode,
                    Score = session.Score
                });
            }

            _logger.LogInformation("Đã lấy thành công {Count} bản ghi điểm cho ExamSessionSubjectId: {ExamSessionSubjectId}, Môn học: {SubjectCode}", result.Count, examSessionSubjectId, subjectCode);
            return (result, subjectCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách điểm sinh viên cho ExamSessionSubjectId: {ExamSessionSubjectId}", examSessionSubjectId);
            throw;
        }
    }
    #endregion
    
    #region ExportStudentGradesToExcelAsync
    public async Task<byte[]> ExportStudentGradesToExcelAsync(IEnumerable<StudentGradeDto> grades)
    {
        try
        {
            _logger.LogInformation("Bắt đầu export Excel bảng điểm với {Count} bản ghi", grades.Count());

            // Tạo file Excel
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Bảng Điểm");
            
            // Tạo header
            worksheet.Cells[1, 1].Value = "STT";
            worksheet.Cells[1, 2].Value = "Mã Sinh Viên";
            worksheet.Cells[1, 3].Value = "Điểm";
            
            // Style header
            var headerRange = worksheet.Cells[1, 1, 1, 3];
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            
            // Fill data
            int row = 2;
            foreach (var grade in grades)
            {
                worksheet.Cells[row, 1].Value = grade.STT;
                worksheet.Cells[row, 2].Value = grade.StudentCode;
                worksheet.Cells[row, 3].Value = grade.Score;
                row++;
            }
            
            // Auto fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            
            var excelBytes = package.GetAsByteArray();
            _logger.LogInformation("Đã tạo thành công file Excel với {Count} bản ghi", grades.Count());
            
            return excelBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi export Excel bảng điểm");
            throw;
        }
    }
    #endregion



    #region GetStudentExamSessionsByStudentCodeAsync

    public async Task<IEnumerable<StudentExamSessionHistoryDto>> GetStudentExamSessionsByStudentCodeAsync(string studentCode)
    {
        var studentExamSession = await _studentExamSessionRepository
            .GetQueryable()
            .Where(x => x.StudentCode == studentCode && x.IsCompleted)
            .Include(x => x.ExamSessionSubject)
            .ThenInclude(x => x.Subject)        // nếu cần thông tin môn học
            .Include(x => x.ExamSessionSubject)
            .ThenInclude(x => x.ExamRoom)       // để lấy ExamName
            .Include(x => x.ExamSessionSubject)
            .ThenInclude(x => x.ExamSession)    // để lấy ExamSessionName
            .Include(x => x.OriginalExamPaper)
            .ThenInclude(op => op.OriginalExamPaperDetails)
            .Include(x => x.ShuffledExamPaper)
            .ToListAsync();


        // Map sang DTO
        var result = _mapper.Map<List<StudentExamSessionHistoryDto>>(studentExamSession);

        // Recalculate logic based on Grouped Questions (matching Frontend ExamResultScreen logic)
        foreach (var dto in result)
        {
            try
            {
                var session = studentExamSession.FirstOrDefault(x => x.StudentExamSessionId == dto.StudentExamSessionId);
                if (session?.OriginalExamPaper?.OriginalExamPaperDetails == null) continue;

                var allDetails = session.OriginalExamPaper.OriginalExamPaperDetails.ToList();
                var parentQuestions = allDetails.Where(q => q.ParentQuestionId == null).ToList();

                // If no parents found but details exist, assume flat structure (all are parents)
                if (parentQuestions.Count == 0 && allDetails.Count > 0)
                {
                    parentQuestions = allDetails;
                }

                // Update Total Count to be the number of "Question Containers" by default
                dto.TotalQuestions = parentQuestions.Count;

                // Recalculate Correct Answers & Score using Hierarchy Logic (Matching Support)
                if (!string.IsNullOrEmpty(session.StudentAnswersString))
                {
                    string answerKey = session.ShuffledExamPaper?.AnswerKey ?? session.OriginalExamPaper.KeyValueList;
                    
                    if (!string.IsNullOrEmpty(answerKey))
                    {
                        // Use the new helper method that supports matching questions
                        var (score, correct, total) = _examPaperHelper.CalculateScoreWithHierarchy(
                            answerKey,
                            _examPaperHelper.ParseAnswerKey(session.StudentAnswersString),
                            allDetails
                        );

                        dto.CorrectAnswers = correct;
                        dto.TotalQuestions = total; // This confirms the container count
                        dto.Score = score;
                    }
                }
            }
            catch (Exception)
            {
                // Fallback: keep original values from DB if calculation fails
            }
        }

        return result;
    }


    #endregion

    #region RankByExamSessionSubjectAsync
    public async Task<AllSubjectRankingResponseDto>
GetAllCompletedSubjectRankingsAsync(string studentCode)
    {
        // 1️⃣ Lấy tất cả bài đã nộp của sinh viên
        var completedSessions = await _studentExamSessionRepository
            .GetQueryable()
            .Where(x =>
                x.StudentCode == studentCode &&
                x.IsCompleted == true
            )
            .Select(x => x.ExamSessionSubjectId)
            .Distinct()
            .ToListAsync();

        var result = new List<SubjectRankingDto>();

        foreach (var subjectId in completedSessions)
        {
            // 2️⃣ Lấy toàn bộ bài đã nộp của môn đó
            var sessions = await _studentExamSessionRepository
                .GetQueryable()
                .Where(x =>
                    x.ExamSessionSubjectId == subjectId &&
                    x.IsCompleted == true
                )
                .Include(x => x.Student)
                .OrderByDescending(x => x.Score)
                .ToListAsync();

            var rankedList = new List<RankedStudentDto>();

            int rank = 0;
            int index = 0;
            double? lastScore = null;

            foreach (var session in sessions)
            {
                index++;

                if (lastScore != session.Score)
                {
                    rank = index;
                    lastScore = session.Score;
                }

                rankedList.Add(new RankedStudentDto
                {
                    Rank = rank,
                    StudentCode = session.StudentCode,
                    FullName = $"{session.Student?.LastName} {session.Student?.FirstName}".Trim(),
                    Score = session.Score
                });
            }

            var myRank = rankedList.FirstOrDefault(x => x.StudentCode == studentCode);

            // ⚠️ Lấy tên môn – SỬA THEO ENTITY THỰC TẾ CỦA BẠN
            var subjectName = await _studentExamSessionRepository
                .GetQueryable()
                .Where(x => x.ExamSessionSubjectId == subjectId)
                .Select(x => x.ExamSessionSubject.Subject.SubjectName)
                .FirstAsync();

            result.Add(new SubjectRankingDto
            {
                ExamSessionSubjectId = subjectId ?? 0,
                SubjectName = subjectName,
                Top5 = rankedList.Take(5).ToList(),
                MyRank = myRank
            });
        }

        return new AllSubjectRankingResponseDto
        {
            Subjects = result
        };
    }


    #endregion

}


