using backend_manage.core.Entities;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.Http;

namespace backend_manage.core.Services.Interfaces
{
    public interface IOriginalExamPaperService
    {

        Task ImportFromXmlAsync(IFormFile file, string originalExamPaperCore);

        Task<OriginalExamPaperDto> GetWithDetailsAsync(string originalExamPaperCore);
        Task CreateShuffledExamPapersAsync(string originalExamPaperCore, int count);
        Task<List<OriginalExamDto>> GetAllAsync();
        /*
        Task ImportFromXmlAsync(Microsoft.AspNetCore.Http.IFormFile file, string OriginalExamPaperCore);
        //



        // Các phương thức mới để kiểm tra câu hỏi cha có câu hỏi con


        Task<List<OriginalExamPaperDetailShufferDto>> ShuffleParentQuestionsAsync(int originalExamPaperId);
        // Phương thức lấy danh sách OriginalExamDto theo SubjectId
        Task<IEnumerable<OriginalExamDto>> GetOriginalExamDtosBySubjectIdAsync(int subjectId);

        // Phương thức lấy câu hỏi cha theo khả năng hoán vị
        Task<(IEnumerable<OriginalExamPaperDetail> ShufflableQuestions, IEnumerable<OriginalExamPaperDetail> NonShufflableQuestions)> GetParentQuestionsAsync(int originalExamPaperId);

        // Phương thức lấy câu hỏi con theo khả năng hoán vị
        Task<(IEnumerable<OriginalExamPaperDetail> ShufflableQuestions, IEnumerable<OriginalExamPaperDetail> NonShufflableQuestions)> GetChildQuestionsAsync(int originalExamPaperDetailId);

        // Phương thức lấy câu hỏi con theo exam paper ID
        Task<(IEnumerable<OriginalExamPaperDetail> ShufflableQuestions, IEnumerable<OriginalExamPaperDetail> NonShufflableQuestions)> GetChildQuestionsByExamPaperAsync(int originalExamPaperId);
        
 
        */
                               // Phương thức lấy danh sách Answers theo OriginalExamPaperId
          Task<IEnumerable<Answers>> GetAnswersByOriginalExamPaperIdAsync(int originalExamPaperId);
          
          // Phương thức lấy danh sách Answers theo OriginalExamPaperDetailId với phân loại khả năng hoán vị
          Task<(IEnumerable<Answers> ShufflableAnswers, IEnumerable<Answers> NonShufflableAnswers)> GetAnswersByOriginalExamPaperDetailIdAsync(int originalExamPaperDetailId);
          
          // Phương thức hoán vị câu trả lời
          Task<IEnumerable<AnswerShufferDto>> ShuffleAnswersAsync(int originalExamPaperDetailId);
      }
} 