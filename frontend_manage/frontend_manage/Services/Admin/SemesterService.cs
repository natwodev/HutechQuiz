using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs.AcademicAffairs;

namespace frontend_manage.Services.Admin
{
    public class AdminSemesterService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public AdminSemesterService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Đường dẫn tương đối, HttpClient.BaseAddress (từ ApiBaseUrl) sẽ được dùng làm gốc.
            _baseUrl = "api/Semester";
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
                Console.WriteLine($"[Admin][SemesterService] GetAllAsync error: {ex.Message}");
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
                Console.WriteLine($"[Admin][SemesterService] GetByIdAsync error: {ex.Message}");
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
                Console.WriteLine($"[Admin][SemesterService] CreateAsync error: {ex.Message}");
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
                Console.WriteLine($"[Admin][SemesterService] UpdateAsync error: {ex.Message}");
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
                Console.WriteLine($"[Admin][SemesterService] DeleteAsync error: {ex.Message}");
                throw;
            }
        }
    }
}

