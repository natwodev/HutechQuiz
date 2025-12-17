using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs.AcademicAffairs;

namespace frontend_manage.Services.Admin
{
    public class AdminExamBatchService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public AdminExamBatchService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Đường dẫn tương đối, HttpClient.BaseAddress (từ ApiBaseUrl) sẽ được dùng làm gốc.
            _baseUrl = "api/ExamBatch";
        }

        public async Task<List<ExamBatchDto>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ExamBatchDto>>(_baseUrl);
                return response ?? new List<ExamBatchDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamBatchService] GetAllAsync error: {ex.Message}");
                return new List<ExamBatchDto>();
            }
        }

        public async Task<ExamBatchDto> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ExamBatchDto>($"{_baseUrl}/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamBatchService] GetByIdAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<ExamBatchDto> CreateAsync(ExamBatchCreateDto examBatch)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(_baseUrl, examBatch);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ExamBatchDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamBatchService] CreateAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<ExamBatchDto> UpdateAsync(int id, ExamBatchUpdateDto examBatch)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/{id}", examBatch);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ExamBatchDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamBatchService] UpdateAsync error: {ex.Message}");
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
                Console.WriteLine($"[Admin][ExamBatchService] DeleteAsync error: {ex.Message}");
                throw;
            }
        }
    }
}

