using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs;
using frontend_manage.DTOs.AcademicAffairs;
using System.Collections.Generic;

namespace frontend_manage.Services.ExamManager
{
    public class ExamManagerService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _shuffledBaseUrl;

        public ExamManagerService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Sử dụng BaseAddress đã được cấu hình trong Program.cs (ApiBaseUrl)
            // Chỉ cần giữ các đường dẫn tương đối, HttpClient sẽ tự kết hợp với BaseAddress.
            _baseUrl = "api/OriginalExamPaper";
            _shuffledBaseUrl = "api/ShuffledExamPaper";
            _subjectBaseUrl = "api/Subject";
        }

        private readonly string _shuffledBaseUrl;
        private readonly string _subjectBaseUrl;

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
                    IsManualCreated = x.IsManualCreated,
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

        public async Task<OriginalExamPaperDto?> CreateOriginalExamPaperAsync(CreateOriginalExamPaperRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/create", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<OriginalExamPaperDto>();
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi khi tạo đề thi: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo đề thi: {ex.Message}", ex);
            }
        }

        public async Task<OriginalExamPaperDto?> UpdateOriginalExamPaperAsync(UpdateOriginalExamPaperRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/update", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<OriginalExamPaperDto>();
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi khi cập nhật đề thi: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật đề thi: {ex.Message}", ex);
            }
        }

        public async Task<OriginalExamPaperDetailDto?> AddQuestionWithAnswersAsync(CreateQuestionWithAnswersRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/add-question", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<OriginalExamPaperDetailDto>();
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi khi thêm câu hỏi: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi thêm câu hỏi: {ex.Message}", ex);
            }
        }

        public async Task<OriginalExamPaperDetailDto?> UpdateQuestionWithAnswersAsync(UpdateQuestionWithAnswersRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/update-question", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<OriginalExamPaperDetailDto>();
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi khi cập nhật câu hỏi: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi cập nhật câu hỏi: {ex.Message}", ex);
            }
        }

        public async Task<List<SubjectDto>> GetAllSubjectsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<SubjectDto>>(_subjectBaseUrl);
                return response ?? new List<SubjectDto>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách môn học: {ex.Message}", ex);
            }
        }

        public async Task<List<OriginalExamPaperListItemDto>> GetManualCreatedExamPapersAsync()
        {
            try
            {
                var allPapers = await GetAllOriginalExamPapersAsync();
                return allPapers.Where(x => x.IsManualCreated).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách đề thi thủ công: {ex.Message}", ex);
            }
        }

        public async Task<string> GenerateRandomOriginalExamPaperCoreAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/generate-random-core");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GenerateRandomCoreResponse>();
                    return result?.OriginalExamPaperCore ?? string.Empty;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi khi sinh mã OriginalExamPaperCore: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi sinh mã OriginalExamPaperCore: {ex.Message}", ex);
            }
        }
    }

    public class GenerateRandomCoreResponse
    {
        public string OriginalExamPaperCore { get; set; } = string.Empty;
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
        public bool IsManualCreated { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalShuffledPapers { get; set; }
    }
}

