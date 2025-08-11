using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.JSInterop;

namespace frontend_manage.Services
{
    /// <summary>
    /// A general-purpose API service that automatically handles cookie vs token authentication
    /// Other services can inherit from this base class
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly CookieHttpService _cookieHttpService;
        private readonly AuthService _authService;

        public ApiService(HttpClient httpClient, CookieHttpService cookieHttpService, AuthService authService)
        {
            _httpClient = httpClient;
            _cookieHttpService = cookieHttpService;
            _authService = authService;
        }

    protected async Task<string> GetAuthType()
    {
        return await _authService.GetAuthType();
    }        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                var authType = await GetAuthType();
                
                if (authType == "cookie")
                {
                    return await _cookieHttpService.GetAsync<T>(endpoint);
                }
                else
                {
                    return await _httpClient.GetFromJsonAsync<T>(endpoint);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<T?> GetByIdAsync<T>(string endpoint, int id)
        {
            return await GetAsync<T>($"{endpoint}/{id}");
        }

        public async Task<T?> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var authType = await GetAuthType();
                
                if (authType == "cookie")
                {
                    return await _cookieHttpService.PostAsync<T>(endpoint, data);
                }
                else
                {
                    var response = await _httpClient.PostAsJsonAsync(endpoint, data);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadFromJsonAsync<T>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PostAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<T?> PutAsync<T>(string endpoint, int id, object data)
        {
            try
            {
                var authType = await GetAuthType();
                string fullEndpoint = $"{endpoint}/{id}";
                
                if (authType == "cookie")
                {
                    return await _cookieHttpService.PutAsync<T>(fullEndpoint, data);
                }
                else
                {
                    var response = await _httpClient.PutAsJsonAsync(fullEndpoint, data);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadFromJsonAsync<T>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PutAsync: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAsync(string endpoint, int id)
        {
            try
            {
                var authType = await GetAuthType();
                string fullEndpoint = $"{endpoint}/{id}";
                
                if (authType == "cookie")
                {
                    await _cookieHttpService.DeleteAsync<object>(fullEndpoint);
                }
                else
                {
                    var response = await _httpClient.DeleteAsync(fullEndpoint);
                    response.EnsureSuccessStatusCode();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteAsync: {ex.Message}");
                throw;
            }
        }
    }
}
