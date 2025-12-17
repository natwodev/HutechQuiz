using frontend_manage.DTOs.AcademicAffairs;
using System.Net.Http.Json;

namespace frontend_manage.Services.Admin
{
    public class ExamRoomService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ExamRoomService(
            HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Đường dẫn tương đối, HttpClient.BaseAddress (từ ApiBaseUrl) sẽ được dùng làm gốc.
            _baseUrl = "api/ExamRoom";
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

