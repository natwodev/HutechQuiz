using System.Security.Claims;
using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.AuthService.Helpers;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace backend_manage.core.Services.AuthService
{
    public class ExamSessionSubjectService : IExamSessionSubjectService
    {
        private readonly IRepository<ExamSessionSubject> _repository;
        private readonly IRepository<Lecturer> _lecturerRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRepository<ExamSessionSubject> _examSessionSubjectRepository;
        private readonly IRepository<ShuffledExamPaper> _shuffledExamPaperRepository;
        private readonly ExamPaperHelper _examPaperHelper;
        private readonly ExamSessionSubjectCacheHelper _examSessionSubjectCacheHelper;
        public ExamSessionSubjectService(
            IRepository<ExamSessionSubject> repository,
            IRepository<Lecturer> lecturerRepository,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor, 
            IRepository<ExamSessionSubject> examSessionSubjectRepository,
            IRepository<ShuffledExamPaper> shuffledExamPaperRepository,
            ExamPaperHelper examPaperHelper,
            ExamSessionSubjectCacheHelper examSessionSubjectCacheHelper
            )
        {
            _repository = repository;
            _lecturerRepository = lecturerRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _examSessionSubjectRepository = examSessionSubjectRepository;
            _shuffledExamPaperRepository = shuffledExamPaperRepository;
            _examPaperHelper = examPaperHelper;
            _examSessionSubjectCacheHelper = examSessionSubjectCacheHelper;
        }

        public async Task<bool> IsOpenAsync(int examSessionSubjectId)
        {
            return await _examSessionSubjectCacheHelper.GetOrComputeIsOpenAsync(examSessionSubjectId);
        }

        public async Task<IEnumerable<ExamSessionSubjectDto>> GetAllAsync()
        {
            var entities = await _repository.GetQueryable()
                .Include(x => x.Subject)
                .Include(x => x.OriginalExamPaper)
                .Include(x => x.ExamRoom)
                .Include(x => x.Monitor)
                .AsSplitQuery()
                .ToListAsync();
            return _mapper.Map<IEnumerable<ExamSessionSubjectDto>>(entities);
        }

        public async Task<PagedResult<ExamSessionSubjectDto>> GetPagedAsync(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _repository.GetQueryable()
                .Include(x => x.Subject)
                .Include(x => x.OriginalExamPaper)
                .Include(x => x.ExamRoom)
                .Include(x => x.Monitor)
                .AsSplitQuery()
                .AsNoTracking();

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ExamSessionSubjectDto>
            {
                Items = _mapper.Map<List<ExamSessionSubjectDto>>(items),
                TotalItems = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ExamSessionSubjectDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetQueryable()
                .Include(x => x.Subject)
                .Include(x => x.OriginalExamPaper)
                .Include(x => x.ExamRoom)
                .Include(x => x.Monitor)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.ExamSessionSubjectId == id);
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
                .Include(x => x.Monitor)
                .FirstOrDefaultAsync(x => x.ExamSessionSubjectId == result.ExamSessionSubjectId);

            return _mapper.Map<ExamSessionSubjectDto>(fullEntity);
        }

        public async Task<ExamSessionSubjectDto> UpdateAsync(int id, ExamSessionSubjectUpdateDto dto)
        {
            var entity = await _repository.GetQueryable()
                .FirstOrDefaultAsync(x => x.ExamSessionSubjectId == id);
            if (entity == null) return null;
            _mapper.Map(dto, entity);
            entity.EndTime = entity.StartTime.AddMinutes(entity.Duration);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật ExamSessionSubject.");
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            entity.Version++; // Tăng version
            var result = await _repository.UpdateAsync(entity);
            return _mapper.Map<ExamSessionSubjectDto>(result);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _repository.GetQueryable()
                .FirstOrDefaultAsync(x => x.ExamSessionSubjectId == id);
            if (entity == null) return false;
            if (entity.IsDeleted) return true;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng xóa ExamSessionSubject.");
            entity.IsDeleted = true;
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            entity.Version++; // Tăng version
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
            entity.Version++; // Tăng version
            await _repository.UpdateAsync(entity);
            return true;
        }

        public async Task<IEnumerable<ExamSessionSubjectRoomDto>> GetAllWithRoomsAsync()
        {
            var entities = await _repository.GetQueryable()
                .Include(x => x.Subject)
                .Include(x => x.ExamRoom)
                .Include(x => x.Monitor)
                .AsSplitQuery()
                .ToListAsync();
            return entities.Select(x => _mapper.Map<ExamSessionSubjectRoomDto>(x));
        }

        public async Task<bool> UpdateExamRoomIdAsync(int examSessionSubjectId, int? examRoomId)
        {
            var entity = await _repository.GetByIdAsync(examSessionSubjectId);
            if (entity == null) return false;
            
            entity.ExamRoomId = examRoomId;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("lecturerCode")?.Value;
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật ExamSessionSubject.");
            
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            entity.Version++; // Tăng version
            await _repository.UpdateAsync(entity);
            return true;
        }

        public async Task UpdateIsActiveAsync(int examSessionSubjectId, bool isActive)
        {
            var entity = await _repository.GetByIdAsync(examSessionSubjectId);
            if (entity == null) 
                throw new ArgumentException($"Không tìm thấy ExamSessionSubject với ID: {examSessionSubjectId}");

            entity.IsActive = isActive;
            var updatedBy = _httpContextAccessor.HttpContext?.User?.FindFirst("lecturerCode")?.Value
                            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(updatedBy))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật trạng thái hoạt động.");
            
            // Lấy ExamSessionSubject và kiểm tra đề gốc
            var examSessionSubject = await _examSessionSubjectRepository.GetQueryable()
                .FirstOrDefaultAsync(x => x.ExamSessionSubjectId == examSessionSubjectId);
            
            
            // Chỉ cache papers khi kích hoạt ca thi (isActive = true)
            if (isActive && examSessionSubject?.OriginalExamPaperId != null)
            {
                var availablePapers = await _shuffledExamPaperRepository.GetQueryable()
                    .Where(p => p.OriginalExamPaperId == examSessionSubject.OriginalExamPaperId && p.IsApproved == true)
                    .ToListAsync();
                
                // Cache các đề thi hoán vị khả dụng cho đề gốc này
                await _examPaperHelper.CacheAvailablePapersAsync(examSessionSubject.OriginalExamPaperId.Value, availablePapers);
            }
            
            
            entity.UpdatedBy = updatedBy;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            entity.Version++;
            await _repository.UpdateAsync(entity);

            // Cập nhật Redis cache cho trạng thái is-open để phản ánh ngay lập tức
            try
            {
                // Tính lại isOpen theo trạng thái mới, bỏ qua ràng buộc khung giờ
                var isOpen = !entity.IsCompleted && entity.IsActive;
                await _examSessionSubjectCacheHelper.SetIsOpenCacheAsync(examSessionSubjectId, isOpen);
            }
            catch
            {
                // Bỏ qua lỗi redis để không ảnh hưởng luồng chính
            }
        }

        public async Task<IEnumerable<ExamSessionSubjectDto>> GetByExamRoomIdAsync(int examRoomId)
        {
            var entities = await _repository.GetQueryable()
                .Where(x => x.ExamRoomId == examRoomId)
                .Include(x => x.Subject)
                .Include(x => x.OriginalExamPaper)
                .Include(x => x.ExamRoom)
                .Include(x => x.Monitor)
                .AsSplitQuery()
                .ToListAsync();
            return _mapper.Map<IEnumerable<ExamSessionSubjectDto>>(entities);
        }

        // Các phương thức mới cho việc phân công giảng viên
        public async Task AssignLecturerAsync(AssignLecturerDto dto)
        {
            var entity = await _repository.GetQueryable()
                .FirstOrDefaultAsync(x => x.ExamSessionSubjectId == dto.ExamSessionSubjectId);
            if (entity == null) 
                throw new ArgumentException($"Không tìm thấy ExamSessionSubject với ID: {dto.ExamSessionSubjectId}");

            // Kiểm tra xem giảng viên có tồn tại không
            var lecturer = await _lecturerRepository.GetQueryable()
                .FirstOrDefaultAsync(x => x.LecturerId == dto.LecturerId);
            if (lecturer == null) 
                throw new ArgumentException($"Không tìm thấy Lecturer với ID: {dto.LecturerId}");

            entity.MonitorId = dto.LecturerId;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng phân công giảng viên.");
            
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            entity.Version++; // Tăng version
            await _repository.UpdateAsync(entity);
        }

        public async Task UnassignLecturerAsync(UnassignLecturerDto dto)
        {
            var entity = await _repository.GetQueryable()
                .FirstOrDefaultAsync(x => x.ExamSessionSubjectId == dto.ExamSessionSubjectId);
            if (entity == null) 
                throw new ArgumentException($"Không tìm thấy ExamSessionSubject với ID: {dto.ExamSessionSubjectId}");

            entity.MonitorId = null;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng hủy phân công giảng viên.");
            
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTimeHelper.GetVietnamTime();
            entity.Version++; // Tăng version
            await _repository.UpdateAsync(entity);
        }

        public async Task<IEnumerable<ExamSessionSubjectDto>> GetByLecturerIdAsync(int lecturerId)
        {
            var entities = await _repository.GetQueryable()
                .Where(x => x.MonitorId == lecturerId)
                .Include(x => x.Subject)
                .Include(x => x.OriginalExamPaper)
                .Include(x => x.ExamRoom)
                .Include(x => x.Monitor)
                .Include(x => x.ExamSession)
                .AsSplitQuery()
                .ToListAsync();
            return _mapper.Map<IEnumerable<ExamSessionSubjectDto>>(entities);
        }

        public async Task<(SubjectExamRoomStatusDto SubjectExamRoomStatus, IEnumerable<StudentExamRoomStatusDto> Students)> GetExamSessionSubjectWithStudentsAsync(int examSessionSubjectId)
        {
            // Lấy ExamSessionSubject với các thông tin liên quan và danh sách sinh viên
            var examSessionSubject = await _repository.GetQueryable()
                .Where(x => x.ExamSessionSubjectId == examSessionSubjectId)
                .Include(x => x.Subject)
                .Include(x => x.ExamRoom)
                .Include(x => x.ExamSession)
                .Include(x => x.StudentExamSessions)
                    .ThenInclude(s => s.Student)
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (examSessionSubject == null)
                throw new ArgumentException($"Không tìm thấy ExamSessionSubject với ID: {examSessionSubjectId}");

            // Map sang DTO sử dụng AutoMapper
            var subjectExamRoomStatusDto = _mapper.Map<SubjectExamRoomStatusDto>(examSessionSubject);
            var studentDtos = _mapper.Map<IEnumerable<StudentExamRoomStatusDto>>(examSessionSubject.StudentExamSessions);

            return (subjectExamRoomStatusDto, studentDtos);
        }

        public async Task<IEnumerable<SubjectExamRoomStatusDto>> GetSubjectExamRoomStatusByLecturerIdAsync(int lecturerId)
        {
            var entities = await _repository.GetQueryable()
                .Where(x => x.MonitorId == lecturerId)
                .Include(x => x.Subject)
                .Include(x => x.ExamRoom)
                .Include(x => x.ExamSession)
                .Include(x => x.Monitor)
                .AsSplitQuery()
                .ToListAsync();

            return _mapper.Map<IEnumerable<SubjectExamRoomStatusDto>>(entities);
        }
    }
} 