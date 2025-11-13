using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.AuthService
{
    public class ExamRoomService : IExamRoomService
    {
        private readonly IRepository<ExamRoom> _repository;
        private readonly IMapper _mapper;

        public ExamRoomService(
            IRepository<ExamRoom> repository,
            IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ExamRoomDto>> GetAllAsync()
        {
            var rooms = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<ExamRoomDto>>(rooms);
        }

        public async Task<ExamRoomDto?> GetByIdAsync(int id)
        {
            var room = await _repository.GetByIdAsync(id);
            return room == null ? null : _mapper.Map<ExamRoomDto>(room);
        }
    }
}

