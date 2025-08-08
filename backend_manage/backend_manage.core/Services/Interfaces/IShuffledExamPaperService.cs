using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface IShuffledExamPaperService
    {
        Task<ShuffledExamPaperDto> GetWithDetailsAsync(string shuffledExamPaperCore);
        Task<bool> DeleteSoftAsync(int shuffledExamPaperId);
    }
} 