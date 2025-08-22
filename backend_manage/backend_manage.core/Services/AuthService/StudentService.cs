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
        StudentImportHelper importHelper)
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
            var examSessionSubjectId = session.ExamSessionSubjectId;
            
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

    #region ImportFromExcelAsyncs
    public async Task<StudentImportResultDto> ImportFromExcelAsyncs(IFormFile file, string examSessionSubjectCore)
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
        return new StudentImportResultDto { StudentsAdded = addedCount, StudentExamSessionsAdded = studentExamSessionAdded };
    }
    #endregion
    
    //đã tối ưu
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
    
    //đã tối ưu
    #region ImportFromExcelStreamAsync
    public async Task<StudentImportResultDto> ImportFromExcelStreamAsync(Stream stream, string examSessionSubjectCore, string userId)
    {
        return await _importHelper.ImportFromExcelStreamAsync(stream, examSessionSubjectCore, userId);
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

    
    //Đang tối ưu lấy được đề và phiên khi k có redis / chưa cập nhật vào db và redis 
    #region StartExamAsync
    public async Task<(StudentExamSessionCacheDto studentExamSessionCacheDto, ShuffledExamPaperDto? shuffledExamPaperDto, OriginalExamPaperDto? originalExamPaperDto)> StartExamAsync(string studentCode, int studentExamSessionId)
    {
        _logger.LogInformation("Bắt đầu lấy đề thi cho sinh viên {StudentCode}, phiên thi {StudentExamSessionId}", studentCode, studentExamSessionId);

        var (studentExamSessionCacheDto, shuffledExamPaperDto, originalExamPaperDto) = await _examPaperHelper.GetStudentExamSessionAndExamPaperAsync(studentCode, studentExamSessionId);

        return (studentExamSessionCacheDto, shuffledExamPaperDto, originalExamPaperDto);
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
                _logger.LogDebug("Đã lấy {Count} phiên thi chưa hoàn thành và còn thời gian làm bài từ Redis cache cho sinh viên {StudentCode}", 
                    cachedSessions.Count(), studentCode);
                return cachedSessions;
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
    public async Task<(IEnumerable<StudentExamRoomStatusDto> Students, SubjectExamRoomStatusDto SubjectInfo)> GetStudentsByExamSessionSubjectAsync(int examSessionSubjectId)
    {
        var query = _studentExamSessionRepository.GetQueryable()
            .Where(ses => ses.ExamSessionSubjectId == examSessionSubjectId)
            .Include(ses => ses.Student)
            .Include(ses => ses.ExamSessionSubject)
                .ThenInclude(ess => ess.Subject)
            .Include(ses => ses.ExamSessionSubject)
                .ThenInclude(ess => ess.ExamRoom)
            .AsSplitQuery();

        var sessions = await query.ToListAsync();

        var students = sessions.Select(x => _mapper.Map<StudentExamRoomStatusDto>(x)).ToList();

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
                ExamSessionEndTime = anySession.ExamSessionEndTime
            };
        }
        else
        {
            // Fallback: query subject info directly
            var ess = await _examSessionSubjectRepository.GetQueryable()
                .Include(x => x.Subject)
                .FirstOrDefaultAsync(x => x.ExamSessionSubjectId == examSessionSubjectId);
            subjectInfo = new SubjectExamRoomStatusDto
            {
                SubjectId = ess?.SubjectId ?? 0,
                SubjectCode = ess?.Subject?.SubjectCore,
                SubjectName = ess?.Subject?.SubjectName,
                RoomName = null,
                Duration = ess?.Duration ?? 0,
                ExamSessionStartTime = ess?.StartTime ?? DateTimeHelper.GetVietnamTime(),
                ExamSessionEndTime = ess?.EndTime ?? DateTimeHelper.GetVietnamTime()
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
                
            
                // Luôn gửi thông báo real-time dựa trên ExamSessionSubjectId
                var (statusList, subjectInfo) = await GetStudentsByExamSessionSubjectAsync(sessionEntity.ExamSessionSubjectId);

                var groupName = $"lecturer_subject_{sessionEntity.ExamSessionSubjectId}";
            
                await _hubContext.Clients
                    .Group(groupName)
                    .SendAsync("RoomStatusUpdated", new StudentListResponse { 
                        Students = statusList.ToList(),
                        Subject = subjectInfo
                    });
                    
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
            var examSessionSubjectId = session.ExamSessionSubjectId;
            
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

    // Helper methods để tái sử dụng code
    #region UpdateSingleAnswerAsync
    public async Task<(bool Success, string Message, string? NewAnswersString)> UpdateSingleAnswerAsync(string studentCode, int studentExamSessionId, int key ,int value)
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
    public async Task<(bool Success, string Message, ExamSubmissionDto? SubmissionData)> SubmitExamAsync(string studentCode, int studentExamSessionId)
    {
        try
        {
            // Sử dụng ExamPaperHelper để nộp bài thi
            var (success, score, message, studentExamSessionDto, answerKey) = await _examPaperHelper.SubmitExam(studentCode, studentExamSessionId);
            
            if (!success)
            {
                _logger.LogWarning("⚠️ Không thể nộp bài thi cho sinh viên {StudentCode}: {Message}", studentCode, message);
                return (false, message, null);
            }
            
            if (studentExamSessionDto == null)
            {
                _logger.LogError("❌ Không thể lấy thông tin phiên thi sau khi nộp bài");
                return (false, "Không thể lấy thông tin phiên thi", null);
            }

            var submissionData = new ExamSubmissionDto
            {
                StudentCode = studentCode,
                ShuffledExamPaperId = studentExamSessionDto.ShuffledExamPaperId ?? 0,
                Score = score,
                CorrectAnswers = studentExamSessionDto.CorrectAnswers,
                TotalQuestions = studentExamSessionDto.TotalQuestions,
                StartTime = studentExamSessionDto.StartTime,
                EndTime = studentExamSessionDto.EndTime ?? DateTimeHelper.GetVietnamTime(),
                StudentAnswersString = studentExamSessionDto.StudentAnswersString,
                AnswerKey = answerKey
            };

            _logger.LogInformation("✅ Hoàn thành nộp bài thi cho sinh viên {StudentCode}. Điểm: {Score}", studentCode, score);
            
           
            
            return (true, message, submissionData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi nộp bài thi cho sinh viên {StudentCode}", studentCode);
            return (false, $"Lỗi hệ thống: {ex.Message}", null);
        }
    }
    #endregion
    
} 


