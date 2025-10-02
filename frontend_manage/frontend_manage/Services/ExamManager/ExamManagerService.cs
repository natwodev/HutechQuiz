using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs;
using frontend_manage.DTOs.ExamManager;
using Microsoft.AspNetCore.Components;

namespace frontend_manage.Services.ExamManager
{
    public class ExamManagerService
    {
        private readonly HttpClient _httpClient;
        private readonly NavigationManager _navigation;

        public ExamManagerService(HttpClient httpClient, NavigationManager navigation)
        {
            _httpClient = httpClient;
            _navigation = navigation;
        }

        public async Task<ImportOriginalExamResponse> ImportOriginalExamXmlAsync(Stream fileStream, string fileName, string originalExamPaperCore)
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
            content.Add(fileContent, "file", fileName);
            content.Add(new StringContent(originalExamPaperCore), "originalExamPaperCore");

            var response = await _httpClient.PostAsync("api/OriginalExamPaper/import-xml", content);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ImportOriginalExamResponse>();
            return result ?? new ImportOriginalExamResponse { Message = "OK" };
        }

        public async Task<OriginalExamPaperDto?> GetOriginalExamWithDetailsAsync(string core)
        {
            return await _httpClient.GetFromJsonAsync<OriginalExamPaperDto>($"api/OriginalExamPaper/{core}/with-details");
        }

        public async Task<CreateShuffledResponse> CreateShuffledPapersAsync(string originalExamPaperCore, int count)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(originalExamPaperCore), "originalExamPaperCore");
            content.Add(new StringContent(count.ToString()), "count");

            var response = await _httpClient.PostAsync("api/OriginalExamPaper/create-shuffled", content);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<CreateShuffledResponse>();
            return result ?? new CreateShuffledResponse { Message = "OK" };
        }

        public async Task<ShuffledExamPaperDto?> GetShuffledExamWithDetailsAsync(string core)
        {
            return await _httpClient.GetFromJsonAsync<ShuffledExamPaperDto>($"api/ShuffledExamPaper/{core}/with-details");
        }

        public async Task<OriginalExamPaperDto?> GetOriginalExamWithDetailsMockAsync()
        {
            var absolute = new Uri(new Uri(_navigation.BaseUri), "mock/original-exam-mock.json");
            return await _httpClient.GetFromJsonAsync<OriginalExamPaperDto>(absolute);
        }

        public async Task<List<ShuffledExamPaperDto>> GetShuffledExamsMockAsync()
        {
            var absolute = new Uri(new Uri(_navigation.BaseUri), "mock/shuffled-exams-mock.json");
            var payload = await _httpClient.GetFromJsonAsync<ShuffledExamsMockPayload>(absolute);
            return payload?.Shuffled ?? new List<ShuffledExamPaperDto>();
        }

        private class ShuffledExamsMockPayload
        {
            public string SubjectName { get; set; }
            public string SubjectCode { get; set; }
            public string OriginalExamPaperCore { get; set; }
            public List<ShuffledExamPaperDto> Shuffled { get; set; } = new();
        }

        public Task<OriginalExamPaperDto> GenerateOriginalExamMockAsync(int questionCount = 100)
        {
            var dto = new OriginalExamPaperDto
            {
                OriginalExamPaperId = 1,
                OriginalExamPaperCore = "CS101-MID-2025",
                Title = "Lập trình C# - Giữa kỳ 2025 (Generated Mock)",
                Description = "Đề thi sinh tự động để test UI",
                SubjectId = 1001,
                DurationMinutes = 60,
                TotalQuestions = questionCount,
                Details = new List<OriginalExamPaperDetailDto>()
            };

            for (int i = 1; i <= questionCount; i++)
            {
                var qId = 1000 + i;
                var question = new OriginalExamPaperDetailDto
                {
                    OriginalExamPaperDetailId = qId,
                    Order = i,
                    QuestionContent = $"Câu {i}: Nội dung câu hỏi mẫu về C# ({i})",
                    CorrectAnswerIndex = 1,
                    ParentQuestionId = null,
                    ChapterId = ((i - 1) / 5) + 1,
                    CanShuffleQuestion = true,
                    AnswerShuffleInfo = null,
                    Answers = new List<AnswerDto>
                    {
                        new AnswerDto { AnswerId = qId * 10 + 1, Order = 1, AnswerContent = "Đáp án đúng", IsCorrect = true, CanShuffleAnswer = true, OriginalExamPaperDetailId = qId },
                        new AnswerDto { AnswerId = qId * 10 + 2, Order = 2, AnswerContent = "Đáp án sai 1", IsCorrect = false, CanShuffleAnswer = true, OriginalExamPaperDetailId = qId },
                        new AnswerDto { AnswerId = qId * 10 + 3, Order = 3, AnswerContent = "Đáp án sai 2", IsCorrect = false, CanShuffleAnswer = true, OriginalExamPaperDetailId = qId }
                    }
                };
                dto.Details.Add(question);
            }

            return Task.FromResult(dto);
        }
    }
}


