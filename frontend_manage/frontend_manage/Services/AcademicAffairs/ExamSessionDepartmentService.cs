using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs.AcademicAffairs;
using Microsoft.Extensions.Configuration;

namespace frontend_manage.Services.AcademicAffairs
{
    public class ExamSessionDepartmentService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ExamSessionDepartmentService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["ApiSettings:BaseUrl"] + "/api/ExamSessionDepartment";
        }

        public async Task<List<ExamSessionDepartmentDto>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ExamSessionDepartmentDto>>(_baseUrl);
                return response ?? new List<ExamSessionDepartmentDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllAsync: {ex.Message}");
                return new List<ExamSessionDepartmentDto>();
            }
        }

        public async Task<ExamSessionDepartmentDto> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ExamSessionDepartmentDto>($"{_baseUrl}/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<ExamSessionDepartmentDto> CreateAsync(ExamSessionDepartmentCreateDto examSessionDepartment)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(_baseUrl, examSessionDepartment);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ExamSessionDepartmentDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<ExamSessionDepartmentDto> UpdateAsync(int id, ExamSessionDepartmentUpdateDto examSessionDepartment)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/{id}", examSessionDepartment);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ExamSessionDepartmentDto>();
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
