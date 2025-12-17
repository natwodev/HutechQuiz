using System.Security.Claims;
using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.Http;

namespace backend_manage.core.Services.AuthService
{
    public class SubjectService : ISubjectService
    {
        private readonly IRepository<Subject> _subjectRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SubjectService(IRepository<Subject> subjectRepository, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _subjectRepository = subjectRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<SubjectDto>> GetAllAsync()
        {
            var subjects = await _subjectRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<SubjectDto>>(subjects);
        }

        public async Task<SubjectDto?> GetByIdAsync(int id)
        {
            var subject = await _subjectRepository.GetByIdAsync(id);
            return subject == null ? null : _mapper.Map<SubjectDto>(subject);
        }

        public async Task<SubjectDto> AddAsync(SubjectCreateDto dto)
        {
            var entity = _mapper.Map<Subject>(dto);

            // Gán thông tin người tạo từ context
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo môn học.");

            entity.CreatedBy = userId;
            entity.CreatedAt = DateTimeHelper.GetVietnamTime();

            var result = await _subjectRepository.AddAsync(entity);
            return _mapper.Map<SubjectDto>(result);
        }

        public async Task<SubjectDto?> UpdateAsync(int id, SubjectUpdateDto dto)
        {
            var entity = await _subjectRepository.GetByIdAsync(id);
            if (entity == null) return null;

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật môn học.");

            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();

            _mapper.Map(dto, entity);
            var result = await _subjectRepository.UpdateAsync(entity);
            return _mapper.Map<SubjectDto>(result);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _subjectRepository.GetByIdAsync(id);
            if (entity == null) return false;
            if (entity.IsDeleted) return true;

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng xóa môn học.");

            entity.IsDeleted = true;
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            await _subjectRepository.UpdateAsync(entity);
            return true;
        }
    }
}

