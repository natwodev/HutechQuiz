using System.Security.Claims;
using System.Linq;
using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace backend_manage.core.Services.AuthService
{
    public class ShuffledExamPaperService : IShuffledExamPaperService
    {
        private readonly IRepository<ShuffledExamPaper> _shuffledExamPaperRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConnectionMultiplexer _redis;

        public ShuffledExamPaperService(
            IRepository<ShuffledExamPaper> shuffledExamPaperRepository,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            IConnectionMultiplexer redis)
        {
            _shuffledExamPaperRepository = shuffledExamPaperRepository;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
            _redis = redis;
        }

        public async Task<ShuffledExamPaperDto> GetWithDetailsAsync(string shuffledExamPaperCore)
        {
            var paper = await _shuffledExamPaperRepository.GetQueryable()
                .Where(x => x.ShuffledExamPaperCore == shuffledExamPaperCore)
                .Include(x => x.OriginalExamPaper)
                    .ThenInclude(o => o.OriginalExamPaperDetails)
                        .ThenInclude(d => d.Answers)
                .Include(x => x.OriginalExamPaper)
                    .ThenInclude(o => o.OriginalExamPaperDetails)
                        .ThenInclude(d => d.ChildQuestions)
                            .ThenInclude(c => c.Answers)
                .Include(x => x.Subject)
                .FirstOrDefaultAsync();
            if (paper == null) return null;
            
            var dto = _mapper.Map<ShuffledExamPaperDto>(paper);
            
            // Debug: Log để kiểm tra QuestionStructure
            System.Diagnostics.Debug.WriteLine($"ShuffledExamPaperCore: {shuffledExamPaperCore}");
            System.Diagnostics.Debug.WriteLine($"QuestionStructure (raw): {paper.QuestionStructure}");
            System.Diagnostics.Debug.WriteLine($"QuestionStructures (parsed) count: {dto?.QuestionStructures?.Count ?? 0}");
            
            return dto;
        }

        public async Task<bool> DeleteSoftAsync(int shuffledExamPaperId)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng xóa đề hoán vị.");
            var paper = await _shuffledExamPaperRepository.GetQueryable()
                .Include(x => x.StudentExamSessions)
                .FirstOrDefaultAsync(x => x.ShuffledExamPaperId == shuffledExamPaperId);
            if (paper == null) return false;
            // Kiểm tra ràng buộc: đã được gán cho sinh viên chưa?
            if (paper.StudentExamSessions != null && paper.StudentExamSessions.Any())
                throw new InvalidOperationException("Đề hoán vị đã được sử dụng, không thể xóa.");
            paper.IsDeleted = true;
            paper.UpdatedAt = DateTimeHelper.GetVietnamTime();
            paper.UpdatedBy = userId;
            await _shuffledExamPaperRepository.UpdateAsync(paper);
            return true;
        }

        public async Task<List<ShuffledExamPaperDto>> GetByOriginalExamPaperCoreAsync(string originalExamPaperCore)
        {
            var papers = await _shuffledExamPaperRepository.GetQueryable()
                .Where(x => x.OriginalExamPaper.OriginalExamPaperCore == originalExamPaperCore && !x.IsDeleted)
                .Include(x => x.OriginalExamPaper)
                .Include(x => x.Subject)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
            
            return papers.Select(x => _mapper.Map<ShuffledExamPaperDto>(x)).ToList();
        }

        public async Task<bool> UpdateAllowViewMaterialsAsync(string shuffledExamPaperCore, bool allowViewMaterials)
        {
            if (string.IsNullOrWhiteSpace(shuffledExamPaperCore))
                throw new ArgumentException("Mã đề hoán vị không hợp lệ");

            var shuffledExamPaper = await _shuffledExamPaperRepository.GetQueryable()
                .FirstOrDefaultAsync(x => x.ShuffledExamPaperCore == shuffledExamPaperCore && !x.IsDeleted);
            
            if (shuffledExamPaper == null)
                throw new Exception($"Không tìm thấy đề hoán vị với mã '{shuffledExamPaperCore}'");

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật đề hoán vị.");

            var now = DateTimeHelper.GetVietnamTime();

            // Cập nhật AllowViewMaterials cho đề hoán vị
            shuffledExamPaper.AllowViewMaterials = allowViewMaterials;
            shuffledExamPaper.UpdatedBy = userId;
            shuffledExamPaper.UpdatedAt = now;
            await _shuffledExamPaperRepository.UpdateAsync(shuffledExamPaper);

            return true;
        }

        /// <summary>
        /// Lấy tất cả đề hoán vị để test
        /// </summary>
        public async Task<List<ShuffledExamPaperDto>> GetAllShuffledPapersForTestAsync()
        {
            var papers = await _shuffledExamPaperRepository.GetQueryable()
                .Where(x => !x.IsDeleted)
                .Include(x => x.OriginalExamPaper)
                .Include(x => x.Subject)
                .OrderByDescending(x => x.CreatedAt)
                .Take(50) // Giới hạn 50 đề gần nhất để tránh quá tải
                .ToListAsync();
            
            return papers.Select(x => _mapper.Map<ShuffledExamPaperDto>(x)).ToList();
        }

        /// <summary>
        /// Lấy chi tiết đề hoán vị để làm bài test
        /// </summary>
        public async Task<TestExamPaperDto?> GetTestDetailsAsync(string shuffledExamPaperCore)
        {
            var paper = await _shuffledExamPaperRepository.GetQueryable()
                .Where(x => x.ShuffledExamPaperCore == shuffledExamPaperCore && !x.IsDeleted)
                .Include(x => x.OriginalExamPaper)
                    .ThenInclude(o => o.OriginalExamPaperDetails)
                        .ThenInclude(d => d.Answers)
                .Include(x => x.OriginalExamPaper)
                    .ThenInclude(o => o.OriginalExamPaperDetails)
                        .ThenInclude(d => d.ChildQuestions)
                            .ThenInclude(c => c.Answers)
                .Include(x => x.Subject)
                .FirstOrDefaultAsync();
            
            if (paper == null) return null;

            var shuffledDto = _mapper.Map<ShuffledExamPaperDto>(paper);
            var originalDto = _mapper.Map<OriginalExamPaperDto>(paper.OriginalExamPaper);
            
            // Get folder name from original exam paper core
            var folderName = paper.OriginalExamPaper.OriginalExamPaperCore?.Split('_').FirstOrDefault() ?? "";

            return new TestExamPaperDto
            {
                ExamPaper = shuffledDto,
                OriginalExamPaper = originalDto,
                DurationMinutes = paper.OriginalExamPaper.DurationMinutes,
                FolderName = folderName
            };
        }
        public async Task<ShuffledExamPaperDto?> GetWithDetailsByIdAsync(int id)
        {
            var paper = await _shuffledExamPaperRepository.GetQueryable()
                .Where(x => x.ShuffledExamPaperId == id && !x.IsDeleted)
                .Include(x => x.OriginalExamPaper)
                    .ThenInclude(o => o.OriginalExamPaperDetails)
                        .ThenInclude(d => d.Answers)
                .Include(x => x.OriginalExamPaper)
                    .ThenInclude(o => o.OriginalExamPaperDetails)
                        .ThenInclude(d => d.ChildQuestions)
                            .ThenInclude(c => c.Answers)
                .Include(x => x.Subject)
                .FirstOrDefaultAsync();
            
            if (paper == null) return null;

            var dto = _mapper.Map<ShuffledExamPaperDto>(paper);

            // Manual mapping fix for QuestionStructures
            if ((dto.QuestionStructures == null || !dto.QuestionStructures.Any()) 
                && !string.IsNullOrEmpty(paper.QuestionStructure))
            {
                try 
                {
                    dto.QuestionStructures = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QuestionStructureDto>>(paper.QuestionStructure) ?? new List<QuestionStructureDto>();
                }
                catch
                {
                    dto.QuestionStructures = new List<QuestionStructureDto>();
                }
            }
            
            return dto;
        }
        
    }
} 