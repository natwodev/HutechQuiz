using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs.AcademicAffairs;

namespace frontend_manage.Services.Admin
{
    public class AdminExamBatchDetailService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public AdminExamBatchDetailService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Đường dẫn tương đối, HttpClient.BaseAddress (từ ApiBaseUrl) sẽ được dùng làm gốc.
            _baseUrl = "api/ExamBatchDetail";
        }

        public async Task<List<ExamBatchDetailDto>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ExamBatchDetailDto>>(_baseUrl);
                return response ?? new List<ExamBatchDetailDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamBatchDetailService] GetAllAsync error: {ex.Message}");
                return new List<ExamBatchDetailDto>();
            }
        }

        public async Task<ExamBatchDetailDto> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ExamBatchDetailDto>($"{_baseUrl}/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamBatchDetailService] GetByIdAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<ExamBatchDetailDto> CreateAsync(ExamBatchDetailCreateDto examBatchDetail)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(_baseUrl, examBatchDetail);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ExamBatchDetailDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamBatchDetailService] CreateAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<ExamBatchDetailDto> UpdateAsync(int id, ExamBatchDetailUpdateDto examBatchDetail)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/{id}", examBatchDetail);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ExamBatchDetailDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamBatchDetailService] UpdateAsync error: {ex.Message}");
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
                Console.WriteLine($"[Admin][ExamBatchDetailService] DeleteAsync error: {ex.Message}");
                throw;
            }
        }
    }
}

