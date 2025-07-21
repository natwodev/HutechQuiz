using System.Linq;
using System.Threading.Tasks;
using backend_manage.DTOs;
using backend_manage.Entities;
using backend_manage.Repositories.Interfaces;
using backend_manage.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System;
using backend_manage.Hubs;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace backend_manage.Services.AuthService
{
    public class ShuffledExamPaperService : IShuffledExamPaperService
    {
        private readonly IRepository<ShuffledExamPaper> _shuffledExamPaperRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ShuffledExamPaperService(IRepository<ShuffledExamPaper> shuffledExamPaperRepository, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _shuffledExamPaperRepository = shuffledExamPaperRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ShuffledExamPaperDto> GetWithDetailsAsync(string shuffledExamPaperCore)
        {
            var paper = await _shuffledExamPaperRepository.GetQueryable()
                .Where(x => x.ShuffledExamPaperCore == shuffledExamPaperCore)
                .Include(x => x.ShuffledExamPaperDetails)
                .ThenInclude(d => d.OriginalExamPaperDetail)
                .Include(x => x.OriginalExamPaper)
                .FirstOrDefaultAsync();
            if (paper == null) return null;
            return _mapper.Map<ShuffledExamPaperDto>(paper);
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
    }
} 