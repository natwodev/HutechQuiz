using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.core.Services.Templates;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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


        #region LoginAsync
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
        #endregion
        
        
        #region AddLecturerAsync
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
        #endregion
        
        #region GetAllLecturersAsync
        public async Task<IEnumerable<LecturerDto>> GetAllLecturersAsync()
        {
            var lecturers = await _lecturerRepository.GetQueryable()
                .Include(l => l.Department)
                .ToListAsync();
            return lecturers.Select(l => _mapper.Map<LecturerDto>(l));
        }
        #endregion

        #region GetByLecturerCodeAsync
        public async Task<LecturerDto> GetByLecturerCodeAsync(string lecturerCode)
        {
            var lecturer = await _lecturerRepository.GetQueryable()
                .Include(l => l.Department)
                .FirstOrDefaultAsync(l => l.LecturerCode == lecturerCode);
            return lecturer == null ? null : _mapper.Map<LecturerDto>(lecturer);
        }
        #endregion
        

        
        #region GetProfileAsync
        public async Task<LecturerDto> GetProfileAsync(string lecturerCode)
        {
            var lecturer = await _lecturerRepository.GetQueryable()
                .Include(l => l.Department)
                .FirstOrDefaultAsync(l => l.LecturerCode == lecturerCode);
            
            return lecturer == null ? null : _mapper.Map<LecturerDto>(lecturer);
        }
        #endregion
        
        #region ForceSubmitAsync
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
        #endregion

        #region ImportFromExcelAsync
        public async Task<LecturerImportResultDto> ImportFromExcelAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return new LecturerImportResultDto { LecturersAdded = 0, Message = "File không hợp lệ" };

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo giảng viên.");

            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            var lecturers = new List<Lecturer>();
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0; // Reset stream position
                using (var package = new ExcelPackage(stream))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                        return new LecturerImportResultDto { LecturersAdded = 0, Message = "File Excel không có worksheet nào" };

                    var worksheet = package.Workbook.Worksheets[0];
                    if (worksheet.Dimension == null)
                        return new LecturerImportResultDto { LecturersAdded = 0, Message = "Worksheet trống" };

                    int rowCount = worksheet.Dimension.Rows;
                    int startRow = 1; // Bắt đầu từ hàng 1 để tìm header

                    // Tìm hàng header (có chứa "Mã giảng viên")
                    for (int i = 1; i <= Math.Min(20, rowCount); i++)
                    {
                        var cellValue = worksheet.Cells[i, 2].Text?.Trim();
                        if (cellValue != null && cellValue.Contains("Mã giảng viên", StringComparison.OrdinalIgnoreCase))
                        {
                            startRow = i + 1; // Hàng sau header
                            break;
                        }
                    }

                    for (int row = startRow; row <= rowCount; row++)
                    {
                        var lecturerCode = worksheet.Cells[row, 2].Text?.Trim();
                        var firstName = worksheet.Cells[row, 3].Text?.Trim();
                        var lastName = worksheet.Cells[row, 4].Text?.Trim();
                        var genderText = worksheet.Cells[row, 5].Text?.Trim();
                        var dateOfBirthText = worksheet.Cells[row, 6].Text?.Trim();
                        var email = worksheet.Cells[row, 7].Text?.Trim();
                        var phoneNumber = worksheet.Cells[row, 8].Text?.Trim();
                        var departmentId = worksheet.Cells[row, 9].Text?.Trim();

                        // Bỏ qua hàng trống hoặc không có mã giảng viên
                        if (string.IsNullOrWhiteSpace(lecturerCode) || 
                            string.IsNullOrWhiteSpace(firstName) || 
                            string.IsNullOrWhiteSpace(lastName) ||
                            string.IsNullOrWhiteSpace(departmentId))
                        {
                            continue; // Bỏ qua dòng không hợp lệ
                        }

                        // Parse Gender
                        bool? gender = null;
                        if (!string.IsNullOrWhiteSpace(genderText))
                        {
                            if (genderText.Equals("Nam", StringComparison.OrdinalIgnoreCase) || 
                                genderText.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                genderText == "1")
                            {
                                gender = true;
                            }
                            else if (genderText.Equals("Nữ", StringComparison.OrdinalIgnoreCase) || 
                                     genderText.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                                     genderText == "0")
                            {
                                gender = false;
                            }
                        }

                        // Parse DateOfBirth
                        DateTime? dateOfBirth = null;
                        if (!string.IsNullOrWhiteSpace(dateOfBirthText))
                        {
                            if (DateTime.TryParseExact(dateOfBirthText, new[] { "dd/MM/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" }, 
                                System.Globalization.CultureInfo.InvariantCulture, 
                                System.Globalization.DateTimeStyles.None, out var parsedDate))
                            {
                                dateOfBirth = parsedDate;
                            }
                        }

                        lecturers.Add(new Lecturer
                        {
                            LecturerCode = lecturerCode,
                            FirstName = firstName,
                            LastName = lastName,
                            Gender = gender,
                            DateOfBirth = dateOfBirth,
                            Email = string.IsNullOrWhiteSpace(email) ? null : email,
                            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber,
                            DepartmentId = departmentId,
                            CreatedBy = userId,
                            CreatedAt = DateTimeHelper.GetVietnamTime()
                        });
                    }
                }
            }

            // Lấy danh sách LecturerCode đã tồn tại
            var existingLecturers = (await _lecturerRepository.GetQueryable()
                .ToListAsync())
                .ToDictionary(l => l.LecturerCode);

            int addedCount = 0;
            foreach (var lecturer in lecturers)
            {
                if (!existingLecturers.ContainsKey(lecturer.LecturerCode))
                {
                    await _lecturerRepository.AddAsync(lecturer);
                    addedCount++;
                }
                else
                {
                    // Nếu đã tồn tại thì cập nhật thông tin
                    var existingLecturer = existingLecturers[lecturer.LecturerCode];
                    existingLecturer.FirstName = lecturer.FirstName;
                    existingLecturer.LastName = lecturer.LastName;
                    existingLecturer.Gender = lecturer.Gender;
                    existingLecturer.DateOfBirth = lecturer.DateOfBirth;
                    existingLecturer.Email = lecturer.Email;
                    existingLecturer.PhoneNumber = lecturer.PhoneNumber;
                    existingLecturer.DepartmentId = lecturer.DepartmentId;
                    existingLecturer.UpdatedBy = userId;
                    existingLecturer.UpdatedAt = DateTimeHelper.GetVietnamTime();
                    await _lecturerRepository.UpdateAsync(existingLecturer);
                }
            }

            return new LecturerImportResultDto 
            { 
                LecturersAdded = addedCount, 
                Message = $"Đã import thành công {addedCount} giảng viên mới. {lecturers.Count - addedCount} giảng viên đã được cập nhật." 
            };
        }
        #endregion

        #region DownloadExcelTemplateAsync
        public async Task<byte[]> DownloadExcelTemplateAsync()
        {
            return await Task.FromResult(LecturerExcelTemplate.Generate());
        }
        #endregion
    }
} 

