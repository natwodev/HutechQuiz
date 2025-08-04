using System.Threading.Tasks;
using backend_manage.DTOs.EPZ;
using backend_manage.DTOs;
using backend_manage.Entities;
using System.Collections.Generic;

namespace backend_manage.Services.Interfaces
{
    public interface IOriginalExamPaperService
    {
        Task ImportFromXmlAsync(Microsoft.AspNetCore.Http.IFormFile file, string OriginalExamPaperCore);
        //Task CreateShuffledExamPapersAsync(string originalExamPaperCore, int count);
        Task<OriginalExamPaperDto> GetWithDetailsAsync(string originalExamPaperCore);


        Task CreateShuffledExamPapersAsync(string originalExamPaperCore, int count);
        Task<IEnumerable<OriginalExamPaperDetail>> GetOriginalExamPaperDetailsByCanShuffleQuestionAsync();
        Task<IEnumerable<OriginalExamPaperDetail>> GetOriginalExamPaperDetailsByCannotShuffleQuestionAsync();
        Task<IEnumerable<OriginalExamPaperDetail>> Shufflepaper();
        
    }
} 