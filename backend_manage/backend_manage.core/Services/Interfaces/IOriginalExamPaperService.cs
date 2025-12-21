using backend_manage.core.Entities;
using backend_manage.shared.DTOs;
using Microsoft.AspNetCore.Http;

namespace backend_manage.core.Services.Interfaces
{
    public interface IOriginalExamPaperService
    {
        Task<OriginalExamPaperDto> CreateAsync(CreateOriginalExamPaperRequest request);
        Task<OriginalExamPaperDto> UpdateAsync(UpdateOriginalExamPaperRequest request);
        Task ImportFromXmlAsync(IFormFile file, string originalExamPaperCore);
        Task ImportFromWordAsync(IFormFile file, string originalExamPaperCore, int subjectId);
        Task ImportFromZipAsync(IFormFile file, string originalExamPaperCore, int subjectId);
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
          
          // Phương thức cập nhật AllowViewMaterials cho đề thi gốc và đồng bộ với đề hoán vị
          Task<bool> UpdateAllowViewMaterialsAsync(string originalExamPaperCore, bool allowViewMaterials);
          
          // Phương thức thêm câu hỏi và đáp án cho đề thi thủ công
          Task<OriginalExamPaperDetailDto> AddQuestionWithAnswersAsync(CreateQuestionWithAnswersRequest request);
          
          // Phương thức cập nhật câu hỏi và đáp án
          Task<OriginalExamPaperDetailDto> UpdateQuestionWithAnswersAsync(UpdateQuestionWithAnswersRequest request);
          
          // Phương thức sinh mã OriginalExamPaperCore ngẫu nhiên 26 chữ cái in hoa
          Task<string> GenerateRandomOriginalExamPaperCoreAsync();
          
          // Phương thức xóa cứng đề thi gốc và tất cả các đề hoán vị liên quan
          // Xóa vĩnh viễn khỏi database (hard delete)
          Task<bool> HardDeleteAsync(int originalExamPaperId);
      }
} 