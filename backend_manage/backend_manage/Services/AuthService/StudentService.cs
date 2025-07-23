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

namespace backend_manage.Services.AuthService;

public class StudentService : IStudentService
{
    private readonly IRepository<Student> _repository;
    private readonly IRepository<StudentExamSession> _studentExamSessionRepository;
    private readonly IRepository<ExamSessionSubject> _examSessionSubjectRepository;
    private readonly IRepository<ExamRoom> _examRoomRepository;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;
    private readonly IHubContext<NotificationHub> _hubContext;
    public StudentService(
        IRepository<Student> repository,
        IRepository<StudentExamSession> studentExamSessionRepository,
        IRepository<ExamSessionSubject> examSessionSubjectRepository,
        IRepository<ExamRoom> examRoomRepository,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        IHubContext<NotificationHub> hubContext)
    {
        _repository = repository;
        _studentExamSessionRepository = studentExamSessionRepository;
        _examSessionSubjectRepository = examSessionSubjectRepository;
        _examRoomRepository = examRoomRepository;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _mapper = mapper;
        _hubContext = hubContext;
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

    public async Task<ShuffledExamPaperDto> StartExamAsync(string studentCode, int examSessionSubjectId)
    {
        // 1. Kiểm tra StudentExamSession đã có mã đề chưa
        var studentExamSession = await _studentExamSessionRepository.GetQueryable()
            .Include(x => x.ShuffledExamPaper)
            .FirstOrDefaultAsync(x => x.StudentCode == studentCode && x.ExamSessionSubjectId == examSessionSubjectId);
        if (studentExamSession == null)
            throw new Exception("Không tìm thấy phiên thi của sinh viên cho môn này.");

        // Nếu đã có mã đề, lấy mã đề đó
        int? shuffledExamPaperId = studentExamSession.ShuffledExamPaperId;
        ShuffledExamPaper shuffledExamPaper = null;
        if (shuffledExamPaperId.HasValue)
        {
            shuffledExamPaper = await _studentExamSessionRepository.GetQueryable()
                .Where(x => x.StudentExamSessionId == studentExamSession.StudentExamSessionId)
                .Select(x => x.ShuffledExamPaper)
                .Include(x => x.ShuffledExamPaperDetails)
                .ThenInclude(d => d.OriginalExamPaperDetail)
                .Include(x => x.OriginalExamPaper)
                .FirstOrDefaultAsync();
        }
        else
        {
            // Nếu chưa có, random 1 mã đề từ ExamSessionSubject
            var examSessionSubject = await _examSessionSubjectRepository.GetQueryable()
                .Include(x => x.ShuffledExamPapers)
                .FirstOrDefaultAsync(x => x.ExamSessionSubjectId == examSessionSubjectId);
            if (examSessionSubject == null || examSessionSubject.ShuffledExamPapers == null || !examSessionSubject.ShuffledExamPapers.Any())
                throw new Exception("Không có đề thi hoán vị cho môn này.");
            var availablePapers = examSessionSubject.ShuffledExamPapers.Where(p => p.IsApproved == true).ToList();
            if (!availablePapers.Any())
                throw new Exception("Không có đề thi hoán vị đã được phê duyệt cho môn này.");
            var random = new Random();
            shuffledExamPaper = availablePapers[random.Next(availablePapers.Count)];
            // Gán mã đề cho sinh viên
            studentExamSession.ShuffledExamPaperId = shuffledExamPaper.ShuffledExamPaperId;
            await _studentExamSessionRepository.UpdateAsync(studentExamSession);
        }

        // 2. Tìm đề theo mã đề (Redis)
        var cache = (IDistributedCache)_httpContextAccessor.HttpContext.RequestServices.GetService(typeof(IDistributedCache));
        string cacheKey = $"shuffled_exam_paper:{shuffledExamPaper.ShuffledExamPaperCore}";
        string cachedPaper = await cache.GetStringAsync(cacheKey);
        ShuffledExamPaperDto paperDto = null;
        if (!string.IsNullOrEmpty(cachedPaper))
        {
            paperDto = System.Text.Json.JsonSerializer.Deserialize<ShuffledExamPaperDto>(cachedPaper);
        }
        else
        {
            // Lấy từ DB (bao gồm details)
            var paper = await _studentExamSessionRepository.GetQueryable()
                .Where(x => x.ShuffledExamPaperId == shuffledExamPaper.ShuffledExamPaperId)
                .Select(x => x.ShuffledExamPaper)
                .Include(x => x.ShuffledExamPaperDetails)
                .ThenInclude(d => d.OriginalExamPaperDetail)
                .Include(x => x.OriginalExamPaper)
                .FirstOrDefaultAsync();
            if (paper == null)
                throw new Exception("Không tìm thấy đề thi hoán vị.");
            // Map sang DTO bằng AutoMapper
            paperDto = _mapper.Map<ShuffledExamPaperDto>(paper);
            // Lưu vào Redis
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6)
            };
            await cache.SetStringAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(paperDto), cacheOptions);
        }
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