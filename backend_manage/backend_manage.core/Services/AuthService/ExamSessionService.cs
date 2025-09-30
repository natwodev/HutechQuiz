using System.Security.Claims;
using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace backend_manage.core.Services.AuthService
{
    public class ExamSessionService : IExamSessionService
    {
        private readonly IRepository<ExamSession> _repository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ExamSessionService(
            IRepository<ExamSession> repository,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<ExamSessionDto>> GetAllAsync()
        {
            var sessions = await _repository.GetQueryable()
                .Include(x => x.ExamBatchDetail)
                .AsNoTracking()
                .ToListAsync();
            return _mapper.Map<IEnumerable<ExamSessionDto>>(sessions);
        }

        public async Task<ExamSessionDto?> GetByIdAsync(int id)
        {
            var session = await _repository.GetByIdAsync(id);
            return session == null ? null : _mapper.Map<ExamSessionDto>(session);
        }

        public async Task<ExamSessionDto> AddAsync(ExamSessionCreateDto dto)
        {
            var entity = _mapper.Map<ExamSession>(dto);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo ExamSession.");
            entity.CreatedBy = userId;
            entity.CreatedAt = DateTimeHelper.GetVietnamTime();
            var result = await _repository.AddAsync(entity);

            // Truy vấn lại entity kèm navigation ExamBatchDetail
            var fullEntity = await _repository.GetQueryable()
                .Include(x => x.ExamBatchDetail)
                .FirstOrDefaultAsync(x => x.ExamSessionId == result.ExamSessionId);

            return _mapper.Map<ExamSessionDto>(fullEntity);
        }

        public async Task<ExamSessionDto> UpdateAsync(int id, ExamSessionUpdateDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật ExamSession.");
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            _mapper.Map(dto, entity);
            var result = await _repository.UpdateAsync(entity);

            // Truy vấn lại entity kèm navigation ExamBatchDetail
            var fullEntity = await _repository.GetQueryable()
                .Include(x => x.ExamBatchDetail)
                .FirstOrDefaultAsync(x => x.ExamSessionId == entity.ExamSessionId);

            return _mapper.Map<ExamSessionDto>(fullEntity);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;
            if (entity.IsDeleted) return true;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng xóa ExamSession.");
            entity.IsDeleted = true;
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            await _repository.UpdateAsync(entity);
            return true;
        }
    }
} 