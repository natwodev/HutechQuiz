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
    public class DepartmentService : IDepartmentService
    {
        private readonly IRepository<Department> _departmentRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DepartmentService(IRepository<Department> departmentRepository, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _departmentRepository = departmentRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<IEnumerable<DepartmentDto>> GetAllAsync()
        {
            var departments = await _departmentRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<DepartmentDto>>(departments);
        }
        public async Task<DepartmentDto?> GetByIdAsync(string id)
        {
            var department = await _departmentRepository.GetByIdAsync(id);
            return department == null ? null : _mapper.Map<DepartmentDto>(department);
        }
        public async Task<DepartmentDto> AddAsync(DepartmentCreateDto dto)
        {
            var entity = _mapper.Map<Department>(dto);

            // ✅ Gán thông tin người tạo từ context
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo khoa.");

            entity.CreatedBy = userId;
            entity.CreatedAt = DateTimeHelper.GetVietnamTime();

            var result = await _departmentRepository.AddAsync(entity);
            return _mapper.Map<DepartmentDto>(result);
        }
        public async Task<DepartmentDto> UpdateAsync(string id, DepartmentUpdateDto dto)
        {
            var entity = await _departmentRepository.GetByIdAsync(id);
            if (entity == null) return null;
            
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật năm học.");

            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            
            _mapper.Map(dto, entity);
            var result = await _departmentRepository.UpdateAsync(entity);
            return _mapper.Map<DepartmentDto>(result);
        }
        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _departmentRepository.GetByIdAsync(id);
            if (entity == null) return false;
            if (entity.IsDeleted) return true;
           
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng xóa khoa.");
           
            entity.IsDeleted = true;
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            await _departmentRepository.UpdateAsync(entity);
            return true;
        }
    }
} 