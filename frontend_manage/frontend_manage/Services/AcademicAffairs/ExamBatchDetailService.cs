using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs.AcademicAffairs;
using Microsoft.Extensions.Configuration;

namespace frontend_manage.Services.AcademicAffairs
{
    public class ExamBatchDetailService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ExamBatchDetailService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = _httpClient.BaseAddress + "api/ExamBatchDetail";
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
                Console.WriteLine($"Error in GetAllAsync: {ex.Message}");
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
                Console.WriteLine($"Error in GetByIdAsync: {ex.Message}");
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
                Console.WriteLine($"Error in CreateAsync: {ex.Message}");
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
