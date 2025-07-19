using System.Linq;
using System.Threading.Tasks;
using backend_manage.DTOs;
using backend_manage.Entities;
using backend_manage.Repositories.Interfaces;
using backend_manage.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace backend_manage.Services.AuthService
{
    public class ShuffledExamPaperService : IShuffledExamPaperService
    {
        private readonly IRepository<ShuffledExamPaper> _shuffledExamPaperRepository;
        private readonly IMapper _mapper;
        public ShuffledExamPaperService(IRepository<ShuffledExamPaper> shuffledExamPaperRepository, IMapper mapper)
        {
            _shuffledExamPaperRepository = shuffledExamPaperRepository;
            _mapper = mapper;
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
    }
} 