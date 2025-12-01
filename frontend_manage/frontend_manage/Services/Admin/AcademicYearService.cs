using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs.AcademicAffairs;
using Microsoft.Extensions.Configuration;

namespace frontend_manage.Services.Admin
{
    public class AdminAcademicYearService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public AdminAcademicYearService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            var baseUrl = configuration["ApiSettings:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = "http://localhost:5163";
            }

            _baseUrl = $"{baseUrl.TrimEnd('/')}/api/AcademicYear";
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
                Console.WriteLine($"[Admin][AcademicYearService] GetAllAsync error: {ex.Message}");
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
                Console.WriteLine($"[Admin][AcademicYearService] GetByIdAsync error: {ex.Message}");
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
                Console.WriteLine($"[Admin][AcademicYearService] CreateAsync error: {ex.Message}");
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
                Console.WriteLine($"[Admin][AcademicYearService] UpdateAsync error: {ex.Message}");
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
                Console.WriteLine($"[Admin][AcademicYearService] DeleteAsync error: {ex.Message}");
                throw;
            }
        }
    }
}

