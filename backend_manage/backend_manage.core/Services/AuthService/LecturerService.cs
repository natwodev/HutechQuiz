using System.Security.Claims;
using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using backend_manage.shared.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace backend_manage.core.Services.AuthService
{
    public class LecturerService : ILecturerService
    {
        private readonly IRepository<Lecturer> _lecturerRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public LecturerService(
            IRepository<Lecturer> lecturerRepository, 
            IMapper mapper, 
            IHttpContextAccessor httpContextAccessor)
        {
            _lecturerRepository = lecturerRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
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
    }
} 

