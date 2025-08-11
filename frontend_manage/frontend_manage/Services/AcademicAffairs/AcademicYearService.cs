using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using frontend_manage.DTOs.AcademicAffairs;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Microsoft.JSInterop;

namespace frontend_manage.Services.AcademicAffairs
{
    public class AcademicYearService
    {
        private readonly HttpClient _httpClient;
        private readonly CookieHttpService _cookieHttpService;
        private readonly string _baseUrl = "api/AcademicYear";
        private readonly AuthService _authService;
        private readonly IJSRuntime _jsRuntime;
        
        // Added a flag to determine if we should use cookie authentication
        private bool _useCookieAuth = true;

        public AcademicYearService(HttpClient httpClient, CookieHttpService cookieHttpService, AuthService authService, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _cookieHttpService = cookieHttpService;
            _authService = authService;
            _jsRuntime = jsRuntime;
        }

        public async Task<List<AcademicYearDto>> GetAllAsync()
        {
            try
            {
                // Always use CookieHttpService when _useCookieAuth is true
                // This ensures cookie credentials are always sent
                if (_useCookieAuth)
                {
                    var response = await _cookieHttpService.GetAsync<List<AcademicYearDto>>(_baseUrl);
                    return response ?? new List<AcademicYearDto>();
                }
                else
                {
                    await EnsureAuthorizationHeader();
                    var response = await _httpClient.GetFromJsonAsync<List<AcademicYearDto>>(_baseUrl);
                    return response ?? new List<AcademicYearDto>();
                }
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
                if (_useCookieAuth)
                {
                    return await _cookieHttpService.GetAsync<AcademicYearDto>($"{_baseUrl}/{id}");
                }
                else
                {
                    await EnsureAuthorizationHeader();
                    return await _httpClient.GetFromJsonAsync<AcademicYearDto>($"{_baseUrl}/{id}");
                }
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
                if (_useCookieAuth)
                {
                    return await _cookieHttpService.PostAsync<AcademicYearDto>(_baseUrl, academicYear);
                }
                else
                {
                    await EnsureAuthorizationHeader();
                    var response = await _httpClient.PostAsJsonAsync(_baseUrl, academicYear);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadFromJsonAsync<AcademicYearDto>();
                }
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
                if (_useCookieAuth)
                {
                    return await _cookieHttpService.PutAsync<AcademicYearDto>($"{_baseUrl}/{id}", academicYear);
                }
                else
                {
                    await EnsureAuthorizationHeader();
                    var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/{id}", academicYear);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadFromJsonAsync<AcademicYearDto>();
                }
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
                if (_useCookieAuth)
                {
                    await _cookieHttpService.DeleteAsync<object>($"{_baseUrl}/{id}");
                }
                else
                {
                    await EnsureAuthorizationHeader();
                    var response = await _httpClient.DeleteAsync($"{_baseUrl}/{id}");
                    response.EnsureSuccessStatusCode();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteAsync: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Ensures that the authorization header is properly set before making a request
        /// This method uses the AuthService to check if the user is authenticated
        /// </summary>
        private async Task EnsureAuthorizationHeader()
        {
            bool isAuthenticated = await _authService.IsAuthenticated();
            if (!isAuthenticated)
            {
                throw new UnauthorizedAccessException("Người dùng chưa đăng nhập");
            }
        }
        
        /// <summary>
        /// Initializes the service by checking if the user is authenticated and setting up
        /// appropriate authentication method
        /// </summary>
        public async Task Initialize()
        {
            bool isAuthenticated = await _authService.IsAuthenticated();
            if (!isAuthenticated)
            {
                throw new UnauthorizedAccessException("Người dùng chưa đăng nhập");
            }
            
            // For this specific use case, we're forcing cookie auth to ensure
            // cookies are sent properly with each request
            _useCookieAuth = true;
        }
    }
}
