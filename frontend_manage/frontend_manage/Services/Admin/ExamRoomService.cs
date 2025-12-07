using frontend_manage.DTOs.AcademicAffairs;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace frontend_manage.Services.Admin
{
    public class ExamRoomService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ExamRoomService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            var baseUrl = GetApiBaseUrl(configuration);
            _baseUrl = $"{baseUrl}/api/ExamRoom";
        }

        private static string GetApiBaseUrl(IConfiguration configuration)
        {
            var apiBaseUrl = configuration["ApiBaseUrl"] ?? throw new InvalidOperationException(
                "ApiBaseUrl chưa được cấu hình trong appsettings.json");
            return apiBaseUrl.TrimEnd('/');
        }

        public async Task<List<ExamRoomDto>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ExamRoomDto>>(_baseUrl);
                return response ?? new List<ExamRoomDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamRoomService] GetAllAsync error: {ex.Message}");
                return new List<ExamRoomDto>();
            }
        }

        public async Task<ExamRoomDto?> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ExamRoomDto>($"{_baseUrl}/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamRoomService] GetByIdAsync error: {ex.Message}");
                throw;
            }
        }
    }
}

