using System.Security.Claims;
using AutoMapper;
using backend_manage.DTOs;
using backend_manage.Entities;
using backend_manage.Hubs;
using backend_manage.Repositories.Interfaces;
using backend_manage.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend_manage.Services.AuthService
{
    public class SemesterService : ISemesterService
    {
        private readonly IRepository<Semester> _semesterRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SemesterService(
            IRepository<Semester> semesterRepository,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _semesterRepository = semesterRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<SemesterDto>> GetAllAsync()
        {
            var repo = _semesterRepository as backend_manage.Repositories.AuthRepository.Repository<Semester>;
            var semesters = await repo.GetQueryable()
                .Include(s => s.AcademicYear)
                .AsNoTracking()
                .ToListAsync();
            return _mapper.Map<IEnumerable<SemesterDto>>(semesters);
        }

        public async Task<SemesterDto?> GetByIdAsync(string id)
        {
            var repo = _semesterRepository as backend_manage.Repositories.AuthRepository.Repository<Semester>;
            if (!int.TryParse(id, out var semesterId)) return null;
            var semester = await repo.GetQueryable()
                .Include(s => s.AcademicYear)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SemesterId == semesterId);
            return semester == null ? null : _mapper.Map<SemesterDto>(semester);
        }

        public async Task<SemesterDto> AddAsync(SemesterCreateDto dto)
        {
            var entity = _mapper.Map<Semester>(dto);
            entity.AcademicYearId = dto.AcademicYearId;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo học kỳ.");
            entity.CreatedBy = userId;
            entity.CreatedAt = DateTimeHelper.GetVietnamTime();
            var result = await _semesterRepository.AddAsync(entity);

            // Truy vấn lại để Include AcademicYear
            var semesterWithYear = await _semesterRepository.GetQueryable()
                .Include(s => s.AcademicYear)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SemesterId == result.SemesterId);

            return _mapper.Map<SemesterDto>(semesterWithYear);
        }

        public async Task<SemesterDto> UpdateAsync(string id, SemesterUpdateDto dto)
        {
            var entity = await _semesterRepository.GetByIdAsync(id);
            if (entity == null) return null;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật học kỳ.");
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            _mapper.Map(dto, entity);
            entity.AcademicYearId = dto.AcademicYearId;
            var result = await _semesterRepository.UpdateAsync(entity);
            return _mapper.Map<SemesterDto>(result);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _semesterRepository.GetByIdAsync(id);
            if (entity == null) return false;
            if (entity.IsDeleted) return true;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng xóa học kỳ.");
            entity.IsDeleted = true;
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            await _semesterRepository.UpdateAsync(entity);
            return true;
        }
    }
} 