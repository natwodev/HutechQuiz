using System.Threading.Tasks;
using backend_manage.DTOs;

namespace backend_manage.Services.Interfaces
{
    public interface IShuffledExamPaperService
    {
        Task<ShuffledExamPaperDto> GetWithDetailsAsync(string shuffledExamPaperCore);
        Task<bool> DeleteSoftAsync(int shuffledExamPaperId);
    }
} 