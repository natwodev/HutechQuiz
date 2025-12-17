using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs;
using frontend_manage.Services;
using Microsoft.JSInterop;
using Microsoft.Extensions.Configuration;

namespace frontend_manage.Services.Admin
{
    public class LecturerService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly AuthService _authService;

        public LecturerService(HttpClient httpClient, AuthService authService)
        {
            _httpClient = httpClient;
            // Đường dẫn tương đối, HttpClient.BaseAddress (từ ApiBaseUrl) sẽ được dùng làm gốc.
            _baseUrl = "api/Lecturer";
            _authService = authService;
        }

        private static string GetApiBaseUrl(IConfiguration configuration)
        {
            var apiBaseUrl = configuration["ApiBaseUrl"] ?? throw new InvalidOperationException(
                "ApiBaseUrl chưa được cấu hình trong appsettings.json");
            return apiBaseUrl.TrimEnd('/');
        }

        public async Task<List<LecturerDto>> GetAllLecturersAsync()
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.GetFromJsonAsync<List<LecturerDto>>(_baseUrl);
                return response ?? new List<LecturerDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllLecturersAsync: {ex.Message}");
                return new List<LecturerDto>();
            }
        }

        public async Task<LecturerDto?> AddLecturerAsync(LecturerCreateDto lecturer)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.PostAsJsonAsync(_baseUrl, lecturer);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<LecturerDto>();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddLecturerAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateLecturerAsync(string lecturerCode, LecturerUpdateDto lecturer)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/{lecturerCode}", lecturer);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateLecturerAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteLecturerAsync(string lecturerCode)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.DeleteAsync($"{_baseUrl}/{lecturerCode}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteLecturerAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<LecturerImportResultDto?> ImportFromExcelAsync(Microsoft.AspNetCore.Components.Forms.IBrowserFile file)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                using var formData = new System.Net.Http.MultipartFormDataContent();
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB max
                formData.Add(new System.Net.Http.StreamContent(stream), "file", file.Name);

                var response = await _httpClient.PostAsync($"{_baseUrl}/import-excel", formData);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LecturerImportResponse>();
                    return new LecturerImportResultDto
                    {
                        LecturersAdded = result?.lecturersAdded ?? 0,
                        Message = result?.message ?? "Import thành công"
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi khi import: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ImportFromExcelAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DownloadExcelTemplateAsync(Microsoft.JSInterop.IJSRuntime jsRuntime)
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.GetAsync($"{_baseUrl}/download-template");
                if (response.IsSuccessStatusCode)
                {
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    var fileName = $"Mau_Giang_Vien_{DateTime.Now:yyyyMMdd}.xlsx";
                    
                    // Download file using JS interop
                    await jsRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(fileBytes));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DownloadExcelTemplateAsync: {ex.Message}");
                return false;
            }
        }
    }

    public class LecturerImportResultDto
    {
        public int LecturersAdded { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class LecturerImportResponse
    {
        public int lecturersAdded { get; set; }
        public string message { get; set; } = string.Empty;
    }
}

