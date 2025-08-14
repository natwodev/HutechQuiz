using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using backend_manage.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using backend_manage.core.Hubs;

namespace backend_manage.core.Services.AuthService
{
    public class ExamRoomLecturerAssignmentService : IExamRoomLecturerAssignmentService
    {
        private readonly IRepository<ExamRoomLecturerAssignment> _repository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ExamRoomLecturerAssignmentService(
            IRepository<ExamRoomLecturerAssignment> repository, 
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<ExamRoomLecturerAssignmentDto>> GetAllAsync()
        {
            var entities = await _repository.GetQueryable()
                .Include(x => x.ExamRoom)
                .Include(x => x.ExamSessionSubject)
                .ThenInclude(x => x.Subject)
                .Include(x => x.Lecturer)
                .Include(x => x.ExamSessionSubject.StudentExamSessions)
                .ToListAsync();
            
            return _mapper.Map<IEnumerable<ExamRoomLecturerAssignmentDto>>(entities);
        }

        public async Task<ExamRoomLecturerAssignmentDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetQueryable()
                .Include(x => x.ExamRoom)
                .Include(x => x.ExamSessionSubject)
                .ThenInclude(x => x.Subject)
                .Include(x => x.Lecturer)
                .Include(x => x.ExamSessionSubject.StudentExamSessions)
                .FirstOrDefaultAsync(x => x.ExamRoomLecturerAssignmentId == id);
            
            return entity == null ? null : _mapper.Map<ExamRoomLecturerAssignmentDto>(entity);
        }
        
        public async Task<IEnumerable<ExamRoomLecturerAssignmentDto>> GetByLecturerIdAsync(int lecturerId)
        {
            var entities = await _repository.GetQueryable()
                .Include(x => x.ExamRoom)
                .Include(x => x.ExamSessionSubject)
                .ThenInclude(x => x.Subject)
                .Include(x => x.Lecturer)
                .Include(x => x.ExamSessionSubject.StudentExamSessions)
                .Where(x => x.LecturerId == lecturerId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ExamRoomLecturerAssignmentDto>>(entities);
        }

        public async Task<IEnumerable<ExamRoomLecturerAssignmentDto>> GetByLecturerCodeAsync(string lecturerCode)
        {
            var entities = await _repository.GetQueryable()
                .Include(x => x.ExamRoom)
                .Include(x => x.ExamSessionSubject)
                .ThenInclude(x => x.Subject)
                .Include(x => x.Lecturer)
                .Include(x => x.ExamSessionSubject.StudentExamSessions)
                .Where(x => x.Lecturer.LecturerCode == lecturerCode)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ExamRoomLecturerAssignmentDto>>(entities);
        }

        public async Task<ExamRoomLecturerAssignmentDto> AddAsync(ExamRoomLecturerAssignmentCreateDto dto)
        {
            // Kiểm tra xem assignment đã tồn tại chưa
            var existingAssignment = await _repository.GetQueryable()
                .FirstOrDefaultAsync(x => x.ExamRoomId == dto.ExamRoomId && 
                                        x.ExamSessionSubjectId == dto.ExamSessionSubjectId &&
                                        x.LecturerId == dto.LecturerId &&
                                        !x.IsDeleted);

            if (existingAssignment != null)
            {
                throw new InvalidOperationException("Giảng viên đã được phân công vào phòng thi này cho môn thi này.");
            }

            var entity = _mapper.Map<ExamRoomLecturerAssignment>(dto);
            
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo assignment.");
            
            entity.CreatedBy = userId;
            entity.CreatedAt = DateTimeHelper.GetVietnamTime();

            var result = await _repository.AddAsync(entity);

            // Lấy entity đầy đủ với các navigation properties để map sang DTO
            var fullEntity = await _repository.GetQueryable()
                .Include(x => x.ExamRoom)
                .Include(x => x.ExamSessionSubject)
                .ThenInclude(x => x.Subject)
                .Include(x => x.Lecturer)
                .Include(x => x.ExamSessionSubject.StudentExamSessions)
                .FirstOrDefaultAsync(x => x.ExamRoomLecturerAssignmentId == result.ExamRoomLecturerAssignmentId);

            return _mapper.Map<ExamRoomLecturerAssignmentDto>(fullEntity);
        }

        public async Task<ExamRoomLecturerAssignmentDto?> UpdateAsync(int id, ExamRoomLecturerAssignmentCreateDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;

            // Kiểm tra xem assignment mới có trùng với assignment khác không
            var existingAssignment = await _repository.GetQueryable()
                .FirstOrDefaultAsync(x => x.ExamRoomId == dto.ExamRoomId && 
                                        x.ExamSessionSubjectId == dto.ExamSessionSubjectId &&
                                        x.LecturerId == dto.LecturerId &&
                                        x.ExamRoomLecturerAssignmentId != id &&
                                        !x.IsDeleted);

            if (existingAssignment != null)
            {
                throw new InvalidOperationException("Giảng viên đã được phân công vào phòng thi này cho môn thi này.");
            }

            entity.ExamRoomId = dto.ExamRoomId;
            entity.ExamSessionSubjectId = dto.ExamSessionSubjectId;
            entity.LecturerId = dto.LecturerId;

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật assignment.");
            
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();

            var result = await _repository.UpdateAsync(entity);

            // Lấy entity đầy đủ với các navigation properties để map sang DTO
            var fullEntity = await _repository.GetQueryable()
                .Include(x => x.ExamRoom)
                .Include(x => x.ExamSessionSubject)
                .ThenInclude(x => x.Subject)
                .Include(x => x.Lecturer)
                .Include(x => x.ExamSessionSubject.StudentExamSessions)
                .FirstOrDefaultAsync(x => x.ExamRoomLecturerAssignmentId == result.ExamRoomLecturerAssignmentId);

            return _mapper.Map<ExamRoomLecturerAssignmentDto>(fullEntity);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;
            if (entity.IsDeleted) return true;

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng xóa assignment.");
            
            entity.IsDeleted = true;
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            
            await _repository.UpdateAsync(entity);
            return true;
        }
    }
} 