using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs.AcademicAffairs;

namespace frontend_manage.Services.AcademicAffairs
{
    public class ExamBatchService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ExamBatchService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _baseUrl = "/api/ExamBatch";
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
                Console.WriteLine($"Error in GetAllAsync: {ex.Message}");
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
                Console.WriteLine($"Error in GetByIdAsync: {ex.Message}");
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
                Console.WriteLine($"Error in CreateAsync: {ex.Message}");
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
                Console.WriteLine($"Error in UpdateAsync: {ex.Message}");
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
                Console.WriteLine($"Error in DeleteAsync: {ex.Message}");
                throw;
            }
        }
    }
}
