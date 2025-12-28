using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using System.Security.Claims;
using backend_manage.core.Entities;
using backend_manage.core.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using backend_manage.core.Hubs;
using backend_manage.core.Data;
using backend_manage.core.Services.AuthService.Helpers;

namespace backend_manage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LecturerController : ControllerBase
    {
        private readonly ILecturerService _lecturerService;
        private readonly IStudentService _studentService;
        private readonly IRepository<StudentExamSession> _studentExamSessionRepository;
        private readonly IRepository<ExamSessionSubject> _examSessionSubjectRepository;
        private readonly IRepository<StudentActivity> _studentActivityRepository;
        private readonly IRepository<Student> _studentRepository;
        private readonly StudentCacheHelper _studentCacheHelper;
        private readonly StudentExamSessionCacheHelper _studentExamSessionCacheHelper;
        private readonly ApplicationDbContext _context;

        public LecturerController(
            ILecturerService lecturerService,
            IStudentService studentService,
            IRepository<StudentExamSession> studentExamSessionRepository,
            IRepository<ExamSessionSubject> examSessionSubjectRepository,
            IRepository<StudentActivity> studentActivityRepository,
            IRepository<Student> studentRepository,
            StudentCacheHelper studentCacheHelper,
            StudentExamSessionCacheHelper studentExamSessionCacheHelper,
            ApplicationDbContext context)
        {
            _lecturerService = lecturerService;
            _studentService = studentService;
            _studentExamSessionRepository = studentExamSessionRepository;
            _examSessionSubjectRepository = examSessionSubjectRepository;
            _studentActivityRepository = studentActivityRepository;
            _studentRepository = studentRepository;
            _studentCacheHelper = studentCacheHelper;
            _studentExamSessionCacheHelper = studentExamSessionCacheHelper;
            _context = context;
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<LecturerDto>> AddLecturer([FromBody] LecturerCreateDto dto)
        {
            var result = await _lecturerService.AddLecturerAsync(dto);
            return CreatedAtAction(nameof(GetByLecturerCode), new { lecturerCode = result.LecturerCode }, result);
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<IEnumerable<LecturerDto>>> GetAllLecturers()
        {
            var result = await _lecturerService.GetAllLecturersAsync();
            return Ok(result);
        }

        [HttpGet("{lecturerCode}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<LecturerDto>> GetByLecturerCode(string lecturerCode)
        {
            var result = await _lecturerService.GetByLecturerCodeAsync(lecturerCode);
            if (result == null) return NotFound();
            return Ok(result);
        }

        // GET: api/lecturer/profile
        [HttpGet("profile")]
        [Authorize(Policy = "LecturerOnly")]
        public async Task<ActionResult<LecturerDto>> GetProfile()
        {
            try
            {
                var lecturerCode = User.FindFirst("lecturerCode")?.Value;
                if (string.IsNullOrEmpty(lecturerCode))
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin giảng viên trong token" });
                }

                var lecturer = await _lecturerService.GetProfileAsync(lecturerCode);
                if (lecturer == null)
                {
                    return NotFound(new { message = "Không tìm thấy thông tin giảng viên" });
                }

                return Ok(lecturer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi server khi lấy thông tin profile" });
            }
        }

        // ============ Monitor features (merged) ============
        // Nộp bài thay cho một sinh viên
        [HttpPost("force-submit")]
        [Authorize(Policy = "LecturerOnly")]
        public async Task<IActionResult> ForceSubmit([FromBody] ForceSubmitRequest dto)
        {
            if (dto == null || dto.StudentExamSessionId <= 0 || string.IsNullOrWhiteSpace(dto.StudentCode))
            {
                return BadRequest(new { message = "Thiếu thông tin yêu cầu." });
            }

            var (success, message) = await _lecturerService.ForceSubmitAsync(
                dto.StudentExamSessionId,
                dto.StudentCode);
            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new { message = "Nộp bài thành công cho sinh viên" });
        }

        public class ForceSubmitRequest
        {
            public int StudentExamSessionId { get; set; }
            public string StudentCode { get; set; } = string.Empty;
        }

        [HttpPost("reset-exam-session-start-time")]
        public async Task<IActionResult> ResetExamSessionStartTime()
        {
            var now = DateTimeHelper.GetVietnamTime();
            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);

            // Xóa tất cả activity logs
            var allActivities = await _studentActivityRepository
                .GetQueryable()
                .ToListAsync();
            
            int activitiesDeleted = 0;
            if (allActivities.Any())
            {
                _context.StudentActivities.RemoveRange(allActivities);
                await _context.SaveChangesAsync();
                activitiesDeleted = allActivities.Count;
            }

            var sessions = await _studentExamSessionRepository
                .GetQueryable()
                .ToListAsync();

            var redisTasks = new List<Task>();

            foreach (var session in sessions)
            {
                session.ExamSessionStartTime = now;
                session.Score = 0;
                session.IsCompleted = false;
                session.StartTime = null;
                session.EndTime = null;
                session.CorrectAnswers = null;
                session.TotalQuestions = null;
                session.StudentAnswersString = string.Empty;
                session.UpdatedAt = now;
                session.UpdatedBy = userId;

                if (!string.IsNullOrEmpty(session.StudentCode))
                {
                    redisTasks.Add(_studentExamSessionCacheHelper.ClearAllStudentSessionsCacheAsync(session.StudentCode));
                }
            }
            _context.StudentExamSessions.UpdateRange(sessions);

            var students = await _studentRepository.GetQueryable().ToListAsync();
            foreach (var student in students)
            {
                student.IsLogin = false;
                student.UpdatedAt = now;
                student.UpdatedBy = userId;
                redisTasks.Add(_studentCacheHelper.RemoveStudentFromCacheAsync(student.StudentCode));
            }
            _context.Students.UpdateRange(students);

            var subjects = await _examSessionSubjectRepository
                .GetQueryable()
                .ToListAsync();

            foreach (var subject in subjects)
            {
                subject.StartTime = now;
                subject.EndTime = subject.Duration > 0 ? now.AddMinutes(subject.Duration) : now;
                subject.UpdatedAt = now;
                subject.UpdatedBy = userId;
            }
            _context.ExamSessionSubjects.UpdateRange(subjects);

            // Lưu tất cả thay đổi DB một lần duy nhất
            await _context.SaveChangesAsync();

            // Đợi tất cả tác vụ Redis hoàn thành
            await Task.WhenAll(redisTasks);

            return Ok(new { 
                message = "Đã reset thời gian, điểm số, trạng thái login/đã thi và xóa tất cả cache Redis", 
                activitiesDeleted = activitiesDeleted,
                studentExamSessionsUpdated = sessions.Count, 
                examSessionSubjectsUpdated = subjects.Count,
                studentsReset = students.Count,
                time = now 
            });
        }

        [HttpPost("import-excel")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ImportExcel([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "File không hợp lệ" });
            }

            try
            {
                var result = await _lecturerService.ImportFromExcelAsync(file);
                return Ok(new 
                { 
                    lecturersAdded = result.LecturersAdded, 
                    message = result.Message ?? "Import giảng viên thành công."
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log chi tiết lỗi để debug
                var errorMessage = $"Lỗi khi import: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" | InnerException: {ex.InnerException.Message}";
                }
                errorMessage += $" | StackTrace: {ex.StackTrace}";
                return StatusCode(500, new { message = errorMessage });
            }
        }

        [HttpGet("download-template")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DownloadTemplate()
        {
            try
            {
                var excelBytes = await _lecturerService.DownloadExcelTemplateAsync();
                string fileName = $"Mau_Giang_Vien_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi tạo file mẫu: {ex.Message}" });
            }
        }
    }
} 