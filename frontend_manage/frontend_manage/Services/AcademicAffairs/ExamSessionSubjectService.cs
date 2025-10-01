using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.DTOs.Common;

namespace frontend_manage.Services.AcademicAffairs
{
    public class ExamSessionSubjectService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ExamSessionSubjectService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _baseUrl = "/api/ExamSessionSubject";
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
                Console.WriteLine($"Error in GetAllAsync: {ex.Message}");
                return new List<ExamSessionSubjectDto>();
            }
        }

        public async Task<PagedResult<ExamSessionSubjectDto>> GetPagedAsync(int page, int pageSize)
        {
            try
            {
                var url = $"{_baseUrl}?page={page}&pageSize={pageSize}";
                var response = await _httpClient.GetFromJsonAsync<PagedResult<ExamSessionSubjectDto>>(url);
                return response ?? new PagedResult<ExamSessionSubjectDto> { Items = new List<ExamSessionSubjectDto>(), TotalItems = 0, Page = page, PageSize = pageSize };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPagedAsync: {ex.Message}");
                return new PagedResult<ExamSessionSubjectDto> { Items = new List<ExamSessionSubjectDto>(), TotalItems = 0, Page = page, PageSize = pageSize };
            }
        }

        public async Task<List<ExamSessionSubjectWithRoomsDto>> GetWithRoomsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ExamSessionSubjectWithRoomsDto>>($"{_baseUrl}/with-rooms");
                return response ?? new List<ExamSessionSubjectWithRoomsDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetWithRoomsAsync: {ex.Message}");
                return new List<ExamSessionSubjectWithRoomsDto>();
            }
        }

        public async Task<ExamSessionSubjectDto> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ExamSessionSubjectDto>($"{_baseUrl}/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<ExamSessionSubjectDto> CreateAsync(ExamSessionSubjectCreateDto examSessionSubject)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(_baseUrl, examSessionSubject);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ExamSessionSubjectDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<ExamSessionSubjectDto> UpdateAsync(int id, ExamSessionSubjectUpdateDto examSessionSubject)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/{id}", examSessionSubject);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ExamSessionSubjectDto>();
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

        public async Task<ExamSessionSubjectDto> UpdateOriginalExamPaperAsync(int id, int originalExamPaperId)
        {
            try
            {
                var response = await _httpClient.PatchAsync(
                    $"{_baseUrl}/{id}/original-exam-paper/{originalExamPaperId}",
                    new StringContent(string.Empty)
                );
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ExamSessionSubjectDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateOriginalExamPaperAsync: {ex.Message}");
                throw;
            }
        }
    }
}
