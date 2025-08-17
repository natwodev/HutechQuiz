using backend_manage.core.Entities;
using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface IOriginalExamPaperService
    {
        Task ImportFromXmlAsync(Microsoft.AspNetCore.Http.IFormFile file, string OriginalExamPaperCore);
        //Task CreateShuffledExamPapersAsync(string originalExamPaperCore, int count);

        Task CreateShuffledExamPapersAsync(string originalExamPaperCore, int count);
        
        // Các phương thức mới để kiểm tra câu hỏi cha có câu hỏi con


        Task<List<OriginalExamPaperDetailShufferDto>> ShuffleParentQuestionsAsync(int originalExamPaperId);        
        // Phương thức lấy danh sách OriginalExamDto theo SubjectId
        Task<IEnumerable<OriginalExamDto>> GetOriginalExamDtosBySubjectIdAsync(int subjectId);
        Task<OriginalExamPaperDto> GetWithDetailsAsync(string originalExamPaperCore);
        
        // Phương thức lấy câu hỏi cha theo khả năng hoán vị
        Task<(IEnumerable<OriginalExamPaperDetail> ShufflableQuestions, IEnumerable<OriginalExamPaperDetail> NonShufflableQuestions)> GetParentQuestionsAsync(int originalExamPaperId);
        
        // Phương thức lấy câu hỏi con theo khả năng hoán vị
        Task<(IEnumerable<OriginalExamPaperDetail> ShufflableQuestions, IEnumerable<OriginalExamPaperDetail> NonShufflableQuestions)> GetChildQuestionsAsync(int originalExamPaperDetailId);
        
        // Phương thức lấy câu hỏi con theo exam paper ID
        Task<(IEnumerable<OriginalExamPaperDetail> ShufflableQuestions, IEnumerable<OriginalExamPaperDetail> NonShufflableQuestions)> GetChildQuestionsByExamPaperAsync(int originalExamPaperId);
    }
} 