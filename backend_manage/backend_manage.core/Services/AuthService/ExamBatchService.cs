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
    public class ExamBatchService : IExamBatchService
    {
        private readonly IRepository<ExamBatch> _examBatchRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ExamBatchService(
            IRepository<ExamBatch> examBatchRepository,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _examBatchRepository = examBatchRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<ExamBatchDto>> GetAllAsync()
        {
            var examBatches = await _examBatchRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ExamBatchDto>>(examBatches);
        }

        public async Task<ExamBatchDto?> GetByIdAsync(string id)
        {
            var examBatch = await _examBatchRepository.GetByIdAsync(id);
            return examBatch == null ? null : _mapper.Map<ExamBatchDto>(examBatch);
        }

        public async Task<ExamBatchDto> AddAsync(ExamBatchCreateDto dto)
        {
            var entity = _mapper.Map<ExamBatch>(dto);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo đợt thi.");
            entity.CreatedBy = userId;
            entity.CreatedAt = DateTimeHelper.GetVietnamTime();
            var result = await _examBatchRepository.AddAsync(entity);

            // Truy vấn lại entity kèm navigation
            var fullEntity = await _examBatchRepository.GetQueryable()
                .Include(x => x.Semester)
                .FirstOrDefaultAsync(x => x.ExamBatchId == result.ExamBatchId);

            return _mapper.Map<ExamBatchDto>(fullEntity);
        }

        public async Task<ExamBatchDto> UpdateAsync(string id, ExamBatchUpdateDto dto)
        {
            var entity = await _examBatchRepository.GetByIdAsync(id);
            if (entity == null) return null;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật đợt thi.");
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            _mapper.Map(dto, entity);
            var result = await _examBatchRepository.UpdateAsync(entity);
            return _mapper.Map<ExamBatchDto>(result);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _examBatchRepository.GetByIdAsync(id);
            if (entity == null) return false;
            if (entity.IsDeleted) return true;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng xóa đợt thi.");
            entity.IsDeleted = true;
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            await _examBatchRepository.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> ToggleIsActiveAsync(string id)
        {
            var entity = await _examBatchRepository.GetByIdAsync(id);
            if (entity == null) return false;
           
            entity.IsActive = !entity.IsActive;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật trạng thái đợt thi.");
          
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            await _examBatchRepository.UpdateAsync(entity);
            return true;
        }
    }
} 