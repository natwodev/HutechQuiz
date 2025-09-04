using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using backend_manage.core.Hubs;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
namespace backend_manage.core.Services.AuthService
{
    public class LecturerService : ILecturerService
    {
        private readonly IRepository<Lecturer> _lecturerRepository;
        private readonly IRepository<StudentExamSession> _studentExamSessionRepository;
        private readonly IStudentService _studentService;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;


        public LecturerService(
            IRepository<Lecturer> lecturerRepository,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            IRepository<StudentExamSession> studentExamSessionRepository,
            IStudentService studentService)
        {
            _lecturerRepository = lecturerRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _studentExamSessionRepository = studentExamSessionRepository;
            _studentService = studentService;
        }

        public async Task<LecturerDto> AddLecturerAsync(LecturerCreateDto dto)
        {
            var lecturer = _mapper.Map<Lecturer>(dto);
            lecturer.CreatedAt = DateTime.UtcNow;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            lecturer.CreatedBy = userId;
            
            // Lưu giảng viên
            await _lecturerRepository.AddAsync(lecturer);

            return _mapper.Map<LecturerDto>(lecturer);
        }

        public async Task<IEnumerable<LecturerDto>> GetAllLecturersAsync()
        {
            var lecturers = await _lecturerRepository.GetQueryable()
                .Include(l => l.Department)
                .ToListAsync();
            return lecturers.Select(l => _mapper.Map<LecturerDto>(l));
        }

        public async Task<LecturerDto> GetByLecturerCodeAsync(string lecturerCode)
        {
            var lecturer = await _lecturerRepository.GetQueryable()
                .Include(l => l.Department)
                .FirstOrDefaultAsync(l => l.LecturerCode == lecturerCode);
            return lecturer == null ? null : _mapper.Map<LecturerDto>(lecturer);
        }

        public async Task<LecturerAuthResultDto> LoginAsync(string lecturerCode1, string lecturerCode2)
        {
            // Kiểm tra mã giảng viên phải khớp nhau (tương tự như sinh viên)
            if (lecturerCode1 != lecturerCode2)
            {
                return new LecturerAuthResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "Mã giảng viên nhập không khớp."
                };
            }

            // Tìm giảng viên theo mã
            var lecturer = await _lecturerRepository.GetQueryable()
                .Include(l => l.Department)
                .FirstOrDefaultAsync(l => l.LecturerCode == lecturerCode1);

            if (lecturer == null)
            {
                return new LecturerAuthResultDto
                {
                    IsSuccess = false,
                    ErrorMessage = "Không tìm thấy giảng viên với mã này."
                };
            }

            // Giảng viên không cần cập nhật trạng thái IsLogin như sinh viên

            // Sinh JWT token tương tự như sinh viên
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JWT:key"] ?? "default_secret_key");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("id", lecturer.LecturerId.ToString()),
                    new Claim("lecturerCode", lecturer.LecturerCode),
                    new Claim("role", "Lecturer")
                }),
                Expires = DateTimeHelper.GetVietnamTime().AddDays(7),
                Issuer = _configuration["JWT:Issuer"],
                Audience = _configuration["JWT:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new LecturerAuthResultDto
            {
                IsSuccess = true,
                Token = tokenString,
                Role = "Lecturer"
            };
        }

        public async Task<LecturerDto> GetProfileAsync(string lecturerCode)
        {
            var lecturer = await _lecturerRepository.GetQueryable()
                .Include(l => l.Department)
                .FirstOrDefaultAsync(l => l.LecturerCode == lecturerCode);
            
            return lecturer == null ? null : _mapper.Map<LecturerDto>(lecturer);
        }

        // ============ Monitor actions ============
        public async Task<(bool Success, string Message)> ForceSubmitAsync(int studentExamSessionId, string studentCode)
        {
            var session = await _studentExamSessionRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.StudentExamSessionId == studentExamSessionId && s.StudentCode == studentCode);

            if (session == null)
                return (false, "Không tìm thấy phiên thi của sinh viên.");

            if (session.IsCompleted)
                return (true, "Phiên thi đã được nộp trước đó.");
            var (success, message) = await _studentService.SubmitExamAsync(studentCode, studentExamSessionId);
            return (success, message);
        }

        
    }
} 

