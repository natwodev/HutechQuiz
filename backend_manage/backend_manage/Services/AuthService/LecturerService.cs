using System;
using System.Threading.Tasks;
using backend_manage.Entities;
using backend_manage.Repositories.Interfaces;
using backend_manage.Services.Interfaces;
using backend_manage.DTOs;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace backend_manage.Services.AuthService
{
    public class LecturerService : ILecturerService
    {
        private readonly IRepository<Lecturer> _lecturerRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public LecturerService(IRepository<Lecturer> lecturerRepository, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _lecturerRepository = lecturerRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<LecturerDto> AddLecturerAsync(LecturerCreateDto dto)
        {
            var lecturer = _mapper.Map<Lecturer>(dto);
            lecturer.CreatedAt = DateTime.UtcNow;
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            lecturer.CreatedBy = userId;
            await _lecturerRepository.AddAsync(lecturer);
            return _mapper.Map<LecturerDto>(lecturer);
        }

        public async Task<IEnumerable<LecturerDto>> GetAllLecturersAsync()
        {
            var lecturers = await _lecturerRepository.GetQueryable()
                .Include(l => l.Department)
                .ToListAsync();
            return lecturers.Select(l => _mapper.Map<LecturerDto>(l));
        }

        public async Task<LecturerDto> GetByLecturerCodeAsync(string lecturerCode)
        {
            var lecturer = await _lecturerRepository.GetQueryable()
                .Include(l => l.Department)
                .FirstOrDefaultAsync(l => l.LecturerCode == lecturerCode);
            return lecturer == null ? null : _mapper.Map<LecturerDto>(lecturer);
        }
    }
} 

