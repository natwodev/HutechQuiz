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
    public class ExamSessionSubjectService : IExamSessionSubjectService
    {
        private readonly IRepository<ExamSessionSubject> _repository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ExamSessionSubjectService(
            IRepository<ExamSessionSubject> repository,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<ExamSessionSubjectDto>> GetAllAsync()
        {
            var entities = await _repository.GetQueryable()
                .Include(x => x.Subject)
                .Include(x => x.OriginalExamPaper)
                .Include(x => x.ExamRoom)
                .AsSplitQuery()
                .ToListAsync();
            return _mapper.Map<IEnumerable<ExamSessionSubjectDto>>(entities);
        }

        public async Task<ExamSessionSubjectDto?> GetByIdAsync(string id)
        {
            if (!int.TryParse(id, out var entityId)) return null;
            var entity = await _repository.GetQueryable()
                .Include(x => x.Subject)
                .Include(x => x.OriginalExamPaper)
                .Include(x => x.ExamRoom)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.ExamSessionSubjectId == entityId);
            return entity == null ? null : _mapper.Map<ExamSessionSubjectDto>(entity);
        }

        public async Task<ExamSessionSubjectDto> AddAsync(ExamSessionSubjectCreateDto dto)
        {
            var entity = _mapper.Map<ExamSessionSubject>(dto);
            entity.EndTime = entity.StartTime.AddMinutes(entity.Duration);
            if (string.IsNullOrEmpty(entity.ExamSessionSubjectCore))
            {
                entity.ExamSessionSubjectCore = $"ESS-{entity.SubjectId}-{DateTimeHelper.GetVietnamTime():yyyyMMddHHmmss}";
            }
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo ExamSessionSubject.");
            entity.CreatedBy = userId;
            entity.CreatedAt = DateTimeHelper.GetVietnamTime();
            var result = await _repository.AddAsync(entity);

            var fullEntity = await _repository.GetQueryable()
                .Include(x => x.Subject)
                .Include(x => x.OriginalExamPaper)
                .Include(x => x.ExamRoom)
                .FirstOrDefaultAsync(x => x.ExamSessionSubjectId == result.ExamSessionSubjectId);

            return _mapper.Map<ExamSessionSubjectDto>(fullEntity);
        }

        public async Task<ExamSessionSubjectDto> UpdateAsync(string id, ExamSessionSubjectUpdateDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;
            _mapper.Map(dto, entity);
            entity.EndTime = entity.StartTime.AddMinutes(entity.Duration);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật ExamSessionSubject.");
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            var result = await _repository.UpdateAsync(entity);
            return _mapper.Map<ExamSessionSubjectDto>(result);
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;
            if (entity.IsDeleted) return true;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng xóa ExamSessionSubject.");
            entity.IsDeleted = true;
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            await _repository.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> UpdateOriginalExamPaperIdAsync(int examSessionSubjectId, int originalExamPaperId)
        {
            var entity = await _repository.GetByIdAsync(examSessionSubjectId);
            if (entity == null) return false;
            entity.OriginalExamPaperId = originalExamPaperId;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật ExamSessionSubject.");
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            await _repository.UpdateAsync(entity);
            return true;
        }

        public async Task<IEnumerable<ExamSessionSubjectRoomDto>> GetAllWithRoomsAsync()
        {
            var entities = await _repository.GetQueryable()
                .Include(x => x.Subject)
                .Include(x => x.ExamRoom)
                .AsSplitQuery()
                .ToListAsync();
            return entities.Select(x => _mapper.Map<ExamSessionSubjectRoomDto>(x));
        }

        public async Task<bool> UpdateExamRoomIdAsync(int examSessionSubjectId, int? examRoomId)
        {
            var entity = await _repository.GetByIdAsync(examSessionSubjectId);
            if (entity == null) return false;
            
            entity.ExamRoomId = examRoomId;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật ExamSessionSubject.");
            
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            await _repository.UpdateAsync(entity);
            return true;
        }

        public async Task<IEnumerable<ExamSessionSubjectDto>> GetByExamRoomIdAsync(int examRoomId)
        {
            var entities = await _repository.GetQueryable()
                .Where(x => x.ExamRoomId == examRoomId)
                .Include(x => x.Subject)
                .Include(x => x.OriginalExamPaper)
                .Include(x => x.ExamRoom)
                .AsSplitQuery()
                .ToListAsync();
            return _mapper.Map<IEnumerable<ExamSessionSubjectDto>>(entities);
        }
    }
} 