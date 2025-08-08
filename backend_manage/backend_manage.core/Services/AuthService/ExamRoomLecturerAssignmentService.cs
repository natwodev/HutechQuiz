using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using backend_manage.shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend_manage.core.Services.AuthService
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