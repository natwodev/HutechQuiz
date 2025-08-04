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
        
        // Các phương thức mới để kiểm tra câu hỏi cha có câu hỏi con
        Task<bool> IsParentQuestionWithChildrenAsync(int questionId);
        
        // Phương thức mới để lấy danh sách câu hỏi con của câu hỏi cha
        Task<IEnumerable<OriginalExamPaperDetail>> GetChildQuestionsByParentIdAsync(int parentQuestionId);
        
        // Phương thức mới để lấy danh sách câu hỏi con cho phép hoán vị của câu hỏi cha
        Task<IEnumerable<OriginalExamPaperDetail>> GetShuffleableChildQuestionsByParentIdAsync(int parentQuestionId);
        
        // Phương thức đối lập: lấy danh sách câu hỏi con KHÔNG cho phép hoán vị của câu hỏi cha
        Task<IEnumerable<OriginalExamPaperDetail>> GetNonShuffleableChildQuestionsByParentIdAsync(int parentQuestionId);
        
        Task<IEnumerable<OriginalExamPaperDetail>> Shufflepaperchild(int parentQuestionId);
    }
} 