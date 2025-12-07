using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs;
using Microsoft.Extensions.Configuration;

namespace frontend_manage.Services.ExamManager
{
    public class ExamManagerService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _shuffledBaseUrl;

        public ExamManagerService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            var apiBaseUrl = GetApiBaseUrl(configuration);
            _baseUrl = $"{apiBaseUrl}/api/OriginalExamPaper";
            _shuffledBaseUrl = $"{apiBaseUrl}/api/ShuffledExamPaper";
        }

        private static string GetApiBaseUrl(IConfiguration configuration)
        {
            var apiBaseUrl = configuration["ApiBaseUrl"] ?? throw new InvalidOperationException(
                "ApiBaseUrl chưa được cấu hình trong appsettings.json");
            return apiBaseUrl.TrimEnd('/');
        }

        public async Task<ImportResultDto?> ImportOriginalExamXmlAsync(Stream fileStream, string fileName, string originalExamPaperCore)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                
                // Thêm file stream
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                content.Add(streamContent, "file", fileName);
                
                // Thêm originalExamPaperCore
                content.Add(new StringContent(originalExamPaperCore), "originalExamPaperCore");

                var response = await _httpClient.PostAsync($"{_baseUrl}/import-xml", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ImportResultDto>();
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi khi import đề thi: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi import đề thi: {ex.Message}", ex);
            }
        }

        public async Task<OriginalExamPaperDto?> GetOriginalExamWithDetailsAsync(string core)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<OriginalExamPaperDto>($"{_baseUrl}/{core}/with-details");
                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy chi tiết đề thi: {ex.Message}", ex);
            }
        }

        public async Task<OriginalExamPaperDto> GenerateOriginalExamMockAsync(int questionCount)
        {
            // Tạo mock data cho testing
            var mock = new OriginalExamPaperDto
            {
                OriginalExamPaperId = 1,
                OriginalExamPaperCore = "MOCK_001",
                Title = "Đề thi Mock",
                Description = "Đề thi mẫu để test",
                SubjectId = 1,
                DurationMinutes = 90,
                TotalQuestions = questionCount,
                Details = new List<OriginalExamPaperDetailDto>()
            };

            for (int i = 1; i <= questionCount; i++)
            {
                mock.Details.Add(new OriginalExamPaperDetailDto
                {
                    OriginalExamPaperDetailId = i,
                    Order = i,
                    QuestionContent = $"Câu hỏi số {i}: Đây là nội dung câu hỏi mẫu để test hiển thị.",
                    CorrectAnswerIndex = 1,
                    ParentQuestionId = null,
                    ChapterId = 1,
                    CanShuffleQuestion = true,
                    Answers = new List<AnswerDto>
                    {
                        new AnswerDto { AnswerId = i * 4 - 3, Order = 1, AnswerContent = "Đáp án A", IsCorrect = true },
                        new AnswerDto { AnswerId = i * 4 - 2, Order = 2, AnswerContent = "Đáp án B", IsCorrect = false },
                        new AnswerDto { AnswerId = i * 4 - 1, Order = 3, AnswerContent = "Đáp án C", IsCorrect = false },
                        new AnswerDto { AnswerId = i * 4, Order = 4, AnswerContent = "Đáp án D", IsCorrect = false }
                    }
                });
            }

            return mock;
        }

        public async Task<CreateShuffledResultDto?> CreateShuffledPapersAsync(string originalExamPaperCore, int count)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                content.Add(new StringContent(originalExamPaperCore), "originalExamPaperCore");
                content.Add(new StringContent(count.ToString()), "count");

                var response = await _httpClient.PostAsync($"{_baseUrl}/create-shuffled", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CreateShuffledResultDto>();
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi khi tạo đề hoán vị: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo đề hoán vị: {ex.Message}", ex);
            }
        }

        public async Task<ShuffledExamPaperDto?> GetShuffledExamWithDetailsAsync(string shuffledExamPaperCore)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ShuffledExamPaperDto>($"{_shuffledBaseUrl}/{shuffledExamPaperCore}/with-details");
                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy chi tiết đề hoán vị: {ex.Message}", ex);
            }
        }

        public async Task<List<ShuffledExamPaperDto>> GetShuffledExamsByOriginalCoreAsync(string originalExamPaperCore)
        {
            try
            {
                // Dùng query parameter để tránh vấn đề với ký tự đặc biệt trong URL
                var encodedCore = Uri.EscapeDataString(originalExamPaperCore);
                var response = await _httpClient.GetFromJsonAsync<List<ShuffledExamPaperDto>>($"{_shuffledBaseUrl}/by-original?originalExamPaperCore={encodedCore}");
                return response ?? new List<ShuffledExamPaperDto>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách đề hoán vị: {ex.Message}", ex);
            }
        }

        public async Task<List<ShuffledExamPaperDto>> GetShuffledExamsMockAsync()
        {
            // Tạo mock data cho testing
            var mockList = new List<ShuffledExamPaperDto>();
            
            for (int i = 1; i <= 5; i++)
            {
                mockList.Add(new ShuffledExamPaperDto
                {
                    ShuffledExamPaperId = i,
                    ShuffledExamPaperCore = $"MOCK_SHUFFLED_{i:D3}",
                    Title = $"Đề hoán vị Mock {i}",
                    OriginalExamPaperId = 1,
                    SubjectId = 1,
                    IsApproved = true,
                    AnswerKey = "",
                    SubjectName = "Lập trình C#",
                    SubjectCode = "CS101",
                    ExamSessionSubjectId = null,
                    QuestionStructures = new List<QuestionStructureDto>()
                });
            }
            
            return mockList;
        }

        public async Task<List<OriginalExamPaperListItemDto>> GetAllOriginalExamPapersAsync()
        {
            try
            {
                var httpResponse = await _httpClient.GetAsync($"{_baseUrl}/list");
                
                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi HTTP {httpResponse.StatusCode}: {errorContent}");
                }
                
                var response = await httpResponse.Content.ReadFromJsonAsync<List<OriginalExamDto>>();
                if (response == null)
                    return new List<OriginalExamPaperListItemDto>();
                
                // Map từ OriginalExamDto sang OriginalExamPaperListItemDto
                return response.Select(x => new OriginalExamPaperListItemDto
                {
                    OriginalExamPaperId = x.OriginalExamPaperId,
                    OriginalExamPaperCore = x.OriginalExamPaperCore,
                    Title = x.Title,
                    Description = x.Description,
                    SubjectId = x.SubjectId,
                    SubjectName = x.SubjectName,
                    IsApproved = x.IsApproved,
                    DurationMinutes = x.DurationMinutes,
                    TotalQuestions = x.TotalQuestions,
                    TotalShuffledPapers = x.TotalShuffledPapers
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách đề thi gốc: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateOriginalExamAllowViewMaterialsAsync(string originalExamPaperCore, bool allowViewMaterials)
        {
            try
            {
                var request = new { AllowViewMaterials = allowViewMaterials };
                var encodedCore = Uri.EscapeDataString(originalExamPaperCore);
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/allow-view-materials/{encodedCore}", request);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi khi cập nhật AllowViewMaterials: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật AllowViewMaterials: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateShuffledExamAllowViewMaterialsAsync(string shuffledExamPaperCore, bool allowViewMaterials)
        {
            try
            {
                var request = new { AllowViewMaterials = allowViewMaterials };
                var encodedCore = Uri.EscapeDataString(shuffledExamPaperCore);
                var response = await _httpClient.PutAsJsonAsync($"{_shuffledBaseUrl}/allow-view-materials/{encodedCore}", request);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi khi cập nhật AllowViewMaterials: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật AllowViewMaterials: {ex.Message}", ex);
            }
        }
    }

    public class ImportResultDto
    {
        public string Message { get; set; } = string.Empty;
    }

    public class CreateShuffledResultDto
    {
        public string Message { get; set; } = string.Empty;
    }

    public class OriginalExamPaperListItemDto
    {
        public int OriginalExamPaperId { get; set; }
        public string OriginalExamPaperCore { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public bool? IsApproved { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalShuffledPapers { get; set; }
    }
}

