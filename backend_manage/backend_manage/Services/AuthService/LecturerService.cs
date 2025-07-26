using System;
using System.Threading.Tasks;
using backend_manage.Entities;
using backend_manage.Repositories.Interfaces;
using backend_manage.Services.Interfaces;
using backend_manage.DTOs;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace backend_manage.Services.AuthService
{
    public class LecturerService : ILecturerService
    {
        private readonly IRepository<Lecturer> _lecturerRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public LecturerService(
            IRepository<Lecturer> lecturerRepository, 
            IMapper mapper, 
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _lecturerRepository = lecturerRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task<LecturerDto> AddLecturerAsync(LecturerCreateDto dto)
        {
            var lecturer = _mapper.Map<Lecturer>(dto);
            lecturer.CreatedAt = DateTime.UtcNow;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            lecturer.CreatedBy = userId;
            
            // Lưu giảng viên trước
            await _lecturerRepository.AddAsync(lecturer);

            // Tạo ApplicationUser cho giảng viên
            var appUser = new ApplicationUser
            {
                UserName = dto.LecturerCode, // Sử dụng LecturerCode làm UserName
                Email = dto.Email,
                EmailConfirmed = true,
                FullName = $"{dto.LastName} {dto.FirstName}",
                PhoneNumber = dto.PhoneNumber
            };

            // Sử dụng LecturerCode làm password
            var password = dto.LecturerCode;
            var result = await _userManager.CreateAsync(appUser, password);
            
            if (result.Succeeded)
            {
                // Cập nhật UserId cho giảng viên
                lecturer.UserId = appUser.Id;
                await _lecturerRepository.UpdateAsync(lecturer);
                
                // Gán role "Lecturer" cho user
                await _userManager.AddToRoleAsync(appUser, "Lecturer");
            }
            else
            {
                // Nếu tạo user thất bại, xóa giảng viên đã tạo
                await _lecturerRepository.DeleteAsync(lecturer);
                throw new InvalidOperationException($"Không thể tạo tài khoản cho giảng viên: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

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
    }
} 

