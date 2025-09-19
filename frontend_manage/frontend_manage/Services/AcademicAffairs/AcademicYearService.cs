using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs.AcademicAffairs;
using Microsoft.Extensions.Configuration;

namespace frontend_manage.Services.AcademicAffairs
{
    public class AcademicYearService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public AcademicYearService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = _httpClient.BaseAddress + "api/AcademicYear";
        }

        public async Task<List<AcademicYearDto>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<AcademicYearDto>>(_baseUrl);
                return response ?? new List<AcademicYearDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllAsync: {ex.Message}");
                return new List<AcademicYearDto>();
            }
        }

        public async Task<AcademicYearDto> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<AcademicYearDto>($"{_baseUrl}/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<AcademicYearDto> CreateAsync(AcademicYearCreateDto academicYear)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(_baseUrl, academicYear);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<AcademicYearDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<AcademicYearDto> UpdateAsync(int id, AcademicYearUpdateDto academicYear)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/{id}", academicYear);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<AcademicYearDto>();
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
