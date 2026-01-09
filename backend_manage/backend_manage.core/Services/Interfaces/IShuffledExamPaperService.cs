using backend_manage.shared.DTOs;

namespace backend_manage.core.Services.Interfaces
{
    public interface IShuffledExamPaperService
    {
        Task<ShuffledExamPaperDto> GetWithDetailsAsync(string shuffledExamPaperCore);
        Task<bool> DeleteSoftAsync(int shuffledExamPaperId);
        Task<List<ShuffledExamPaperDto>> GetByOriginalExamPaperCoreAsync(string originalExamPaperCore);
        Task<bool> UpdateAllowViewMaterialsAsync(string shuffledExamPaperCore, bool allowViewMaterials);
        
        /// <summary>
        /// Lấy tất cả đề hoán vị để test (không cần student session)
        /// </summary>
        Task<List<ShuffledExamPaperDto>> GetAllShuffledPapersForTestAsync();
        
        /// <summary>
        /// Lấy chi tiết đề hoán vị để làm bài test (không cần student session)
        /// Trả về cả originalExamPaper với details và answers
        /// </summary>
        Task<TestExamPaperDto?> GetTestDetailsAsync(string shuffledExamPaperCore);
        Task<ShuffledExamPaperDto?> GetWithDetailsByIdAsync(int id);
    }
} 