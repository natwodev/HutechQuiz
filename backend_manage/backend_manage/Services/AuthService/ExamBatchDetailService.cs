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
    public class ExamBatchDetailService : IExamBatchDetailService
    {
        private readonly IRepository<ExamBatchDetail> _repository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ExamBatchDetailService(
            IRepository<ExamBatchDetail> repository,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<ExamBatchDetailDto>> GetAllAsync()
        {
            var details = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<ExamBatchDetailDto>>(details);
        }

        public async Task<ExamBatchDetailDto?> GetByIdAsync(string id)
        {
            var detail = await _repository.GetByIdAsync(id);
            return detail == null ? null : _mapper.Map<ExamBatchDetailDto>(detail);
        }

        public async Task<ExamBatchDetailDto> AddAsync(ExamBatchDetailCreateDto dto)
        {
            var entity = _mapper.Map<ExamBatchDetail>(dto);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo ExamBatchDetail.");
            entity.CreatedBy = userId;
            entity.CreatedAt = DateTimeHelper.GetVietnamTime();
            var result = await _repository.AddAsync(entity);

            // Truy vấn lại entity kèm navigation ExamBatch
            var fullEntity = await _repository.GetQueryable()
                .Include(x => x.ExamBatch)
                .FirstOrDefaultAsync(x => x.ExamBatchDetailId == result.ExamBatchDetailId);

            return _mapper.Map<ExamBatchDetailDto>(fullEntity);
        }

        public async Task<ExamBatchDetailDto> UpdateAsync(string id, ExamBatchDetailUpdateDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật ExamBatchDetail.");
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            _mapper.Map(dto, entity);
            var result = await _repository.UpdateAsync(entity);
            return _mapper.Map<ExamBatchDetailDto>(result);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;
            if (entity.IsDeleted) return true;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng xóa ExamBatchDetail.");
            entity.IsDeleted = true;
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            await _repository.UpdateAsync(entity);
            return true;
        }
    }
} 