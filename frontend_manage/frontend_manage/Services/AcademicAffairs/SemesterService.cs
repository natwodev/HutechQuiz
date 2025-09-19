using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs.AcademicAffairs;
using Microsoft.Extensions.Configuration;

namespace frontend_manage.Services.AcademicAffairs
{
    public class SemesterService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public SemesterService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = _httpClient.BaseAddress + "api/Semester";
        }

        public async Task<List<SemesterDto>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<SemesterDto>>(_baseUrl);
                return response ?? new List<SemesterDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllAsync: {ex.Message}");
                return new List<SemesterDto>();
            }
        }

        public async Task<SemesterDto> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<SemesterDto>($"{_baseUrl}/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<SemesterDto> CreateAsync(SemesterCreateDto semester)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(_baseUrl, semester);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<SemesterDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<SemesterDto> UpdateAsync(int id, SemesterUpdateDto semester)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/{id}", semester);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<SemesterDto>();
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
