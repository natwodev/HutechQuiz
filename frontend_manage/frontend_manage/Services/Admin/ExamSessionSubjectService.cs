using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs.AcademicAffairs;
using Microsoft.Extensions.Configuration;

namespace frontend_manage.Services.Admin
{
    public class AdminExamSessionSubjectService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public AdminExamSessionSubjectService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            var baseUrl = GetApiBaseUrl(configuration);
            _baseUrl = $"{baseUrl}/api/ExamSessionSubject";
        }

        private static string GetApiBaseUrl(IConfiguration configuration)
        {
            var apiBaseUrl = configuration["ApiBaseUrl"] ?? throw new InvalidOperationException(
                "ApiBaseUrl chưa được cấu hình trong appsettings.json");
            return apiBaseUrl.TrimEnd('/');
        }

        public async Task<List<ExamSessionSubjectDto>> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ExamSessionSubjectDto>>(_baseUrl);
                return response ?? new List<ExamSessionSubjectDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamSessionSubjectService] GetAllAsync error: {ex.Message}");
                return new List<ExamSessionSubjectDto>();
            }
        }

        public async Task<ExamSessionSubjectDto?> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ExamSessionSubjectDto>($"{_baseUrl}/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamSessionSubjectService] GetByIdAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<ExamSessionSubjectDto> CreateAsync(ExamSessionSubjectCreateDto payload)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(_baseUrl, payload);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ExamSessionSubjectDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamSessionSubjectService] CreateAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<ExamSessionSubjectDto> UpdateAsync(int id, ExamSessionSubjectUpdateDto payload)
        {
            try
            {
                // Map frontend DTO to backend DTO format
                var backendDto = new
                {
                    payload.ExamSessionId,
                    payload.SubjectId,
                    payload.Duration,
                    payload.OriginalExamPaperId,
                    payload.IsCompleted,
                    payload.StartTime,
                    payload.EndTime,
                    payload.ExamSessionSubjectCore,
                    payload.ExamRoomId,
                    payload.MonitorId
                };
                
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/{id}", backendDto);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ExamSessionSubjectDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamSessionSubjectService] UpdateAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task AssignMonitorAsync(int examSessionSubjectId, int lecturerId)
        {
            try
            {
                var payload = new
                {
                    ExamSessionSubjectId = examSessionSubjectId,
                    LecturerId = lecturerId
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/assign-lecturer", payload);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamSessionSubjectService] AssignMonitorAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task UnassignMonitorAsync(int examSessionSubjectId)
        {
            try
            {
                var payload = new
                {
                    ExamSessionSubjectId = examSessionSubjectId
                };

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/unassign-lecturer", payload);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin][ExamSessionSubjectService] UnassignMonitorAsync error: {ex.Message}");
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
                Console.WriteLine($"[Admin][ExamSessionSubjectService] DeleteAsync error: {ex.Message}");
                throw;
            }
        }
    }
}

