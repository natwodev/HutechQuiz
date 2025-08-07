using System.Security.Claims;
using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using backend_manage.shared.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace backend_manage.core.Services.AuthService
{
    public class ExamSessionDepartmentService : IExamSessionDepartmentService
    {
        private readonly IRepository<ExamSessionDepartment> _repository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ExamSessionDepartmentService(
            IRepository<ExamSessionDepartment> repository,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<ExamSessionDepartmentDto>> GetAllAsync()
        {
            var entities = await _repository.GetQueryable()
                .Include(x => x.Department)
                .Include(x => x.ExamSession)
                .ToListAsync();
            return _mapper.Map<IEnumerable<ExamSessionDepartmentDto>>(entities);
        }

        public async Task<ExamSessionDepartmentDto?> GetByIdAsync(string id)
        {
            if (!int.TryParse(id, out var entityId)) return null;
            var entity = await _repository.GetQueryable()
                .Include(x => x.Department)
                .Include(x => x.ExamSession)
                .FirstOrDefaultAsync(x => x.ExamSessionDepartmentId == entityId);
            return entity == null ? null : _mapper.Map<ExamSessionDepartmentDto>(entity);
        }

        public async Task<ExamSessionDepartmentDto> AddAsync(ExamSessionDepartmentCreateDto dto)
        {
            var entity = _mapper.Map<ExamSessionDepartment>(dto);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo ExamSessionDepartment.");
            entity.CreatedBy = userId;
            entity.CreatedAt = DateTimeHelper.GetVietnamTime();
            var result = await _repository.AddAsync(entity);

            var fullEntity = await _repository.GetQueryable()
                .Include(x => x.Department)
                .Include(x => x.ExamSession)
                .FirstOrDefaultAsync(x => x.ExamSessionDepartmentId == result.ExamSessionDepartmentId);

            return _mapper.Map<ExamSessionDepartmentDto>(fullEntity);
        }

        public async Task<ExamSessionDepartmentDto> UpdateAsync(string id, ExamSessionDepartmentUpdateDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật ExamSessionDepartment.");
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            _mapper.Map(dto, entity);
            var result = await _repository.UpdateAsync(entity);
            return _mapper.Map<ExamSessionDepartmentDto>(result);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;
            if (entity.IsDeleted) return true;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng xóa ExamSessionDepartment.");
            entity.IsDeleted = true;
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            await _repository.UpdateAsync(entity);
            return true;
        }
    }
} 