using System.Collections.Generic;
using System.Threading.Tasks;
using backend_manage.DTOs;
using backend_manage.Entities;
using backend_manage.Repositories.Interfaces;
using backend_manage.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace backend_manage.Services.AuthService
{
    public class ExamRoomLecturerAssignmentService : IExamRoomLecturerAssignmentService
    {
        private readonly IRepository<ExamRoomLecturerAssignment> _repository;
        private readonly IMapper _mapper;
        public ExamRoomLecturerAssignmentService(IRepository<ExamRoomLecturerAssignment> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ExamRoomLecturerAssignmentDto>> GetAllAsync()
        {
            var entities = await _repository.GetQueryable()
                .Include(x => x.ExamRoom)
                .Include(x => x.ExamSessionSubject)
                .ThenInclude(x => x.Subject)
                .Include(x => x.Lecturer)
                .ToListAsync();
            return entities.Select(x => _mapper.Map<ExamRoomLecturerAssignmentDto>(x));
        }

        public async Task<ExamRoomLecturerAssignmentDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetQueryable()
                .Include(x => x.ExamRoom)
                .Include(x => x.ExamSessionSubject)
                .ThenInclude(x => x.Subject)
                .Include(x => x.Lecturer)
                .FirstOrDefaultAsync(x => x.ExamRoomLecturerAssignmentId == id);
            return entity == null ? null : _mapper.Map<ExamRoomLecturerAssignmentDto>(entity);
        }
        
        public async Task<IEnumerable<ExamRoomLecturerAssignmentDto>> GetByLecturerIdAsync(string applicationUserId)
        {
            var entities = await _repository.GetQueryable()
                .Include(x => x.ExamRoom)
                .Include(x => x.ExamSessionSubject)
                .ThenInclude(x => x.Subject)
                .Include(x => x.Lecturer)
                .Where(x => x.Lecturer.UserId == applicationUserId)
                .ToListAsync();

            return entities.Select(x => _mapper.Map<ExamRoomLecturerAssignmentDto>(x));
        }


    }
} 