using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs.AcademicAffairs;
using Microsoft.Extensions.Configuration;

namespace frontend_manage.Services.AcademicAffairs
{
    public class ExamSessionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ExamSessionService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = _httpClient.BaseAddress + "api/ExamSession";
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
                Console.WriteLine($"Error in GetAllAsync: {ex.Message}");
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
                Console.WriteLine($"Error in GetByIdAsync: {ex.Message}");
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
                Console.WriteLine($"Error in CreateAsync: {ex.Message}");
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
