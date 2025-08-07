using System.Security.Claims;
using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using backend_manage.shared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace backend_manage.core.Services.AuthService
{
    public class AcademicYearService : IAcademicYearService
    {
        private readonly IRepository<AcademicYear> _academicYearRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AcademicYearService(
            IRepository<AcademicYear> academicYearRepository,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _academicYearRepository = academicYearRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<AcademicYearDto>> GetAllAsync()
        {
            var academicYears = await _academicYearRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<AcademicYearDto>>(academicYears);
        }

        public async Task<AcademicYearDto?> GetByIdAsync(string id)
        {
            var academicYear = await _academicYearRepository.GetByIdAsync(id);
            return academicYear == null ? null : _mapper.Map<AcademicYearDto>(academicYear);
        }

        public async Task<AcademicYearDto> AddAsync(AcademicYearCreateDto dto)
        {
            var entity = _mapper.Map<AcademicYear>(dto);

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo năm học.");

            entity.CreatedBy = userId;
            entity.CreatedAt = DateTimeHelper.GetVietnamTime();

            var result = await _academicYearRepository.AddAsync(entity);
            return _mapper.Map<AcademicYearDto>(result);
        }

        public async Task<AcademicYearDto> UpdateAsync(string id, AcademicYearUpdateDto dto)
        {
            var entity = await _academicYearRepository.GetByIdAsync(id);
            if (entity == null) return null;
            
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật năm học.");

            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();

            _mapper.Map(dto, entity);
            var result = await _academicYearRepository.UpdateAsync(entity);
            return _mapper.Map<AcademicYearDto>(result);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _academicYearRepository.GetByIdAsync(id);
            if (entity == null) return false;
            if (entity.IsDeleted) return true;
            
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng xóa năm học.");
          
            entity.IsDeleted = true;
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
         
            await _academicYearRepository.UpdateAsync(entity);
            return true;
        }
    }
} 