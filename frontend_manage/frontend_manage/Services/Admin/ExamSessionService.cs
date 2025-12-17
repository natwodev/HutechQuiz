using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs.AcademicAffairs;

namespace frontend_manage.Services.Admin
{
    public class AdminExamSessionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public AdminExamSessionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Đường dẫn tương đối, HttpClient.BaseAddress (từ ApiBaseUrl) sẽ được dùng làm gốc.
            _baseUrl = "api/ExamSession";
        }

        public async Task<List<ExamSessionDto>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ExamSessionDto>>(_baseUrl);
                return response ?? new List<ExamSessionDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamSessionService] GetAllAsync error: {ex.Message}");
                return new List<ExamSessionDto>();
            }
        }

        public async Task<ExamSessionDto> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ExamSessionDto>($"{_baseUrl}/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamSessionService] GetByIdAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<ExamSessionDto> CreateAsync(ExamSessionCreateDto examSession)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(_baseUrl, examSession);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ExamSessionDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamSessionService] CreateAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<ExamSessionDto> UpdateAsync(int id, ExamSessionUpdateDto examSession)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/{id}", examSession);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ExamSessionDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamSessionService] UpdateAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamSessionService] DeleteAsync error: {ex.Message}");
                throw;
            }
        }
    }
}

