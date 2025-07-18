using System.Threading.Tasks;
using backend_manage.DTOs.EPZ;

namespace backend_manage.Services.Interfaces
{
    public interface IOriginalExamPaperService
    {
        Task ImportFromXmlAsync(Microsoft.AspNetCore.Http.IFormFile file, string OriginalExamPaperCore);
        Task CreateShuffledExamPapersAsync(string originalExamPaperCore, int count);
    }
} 