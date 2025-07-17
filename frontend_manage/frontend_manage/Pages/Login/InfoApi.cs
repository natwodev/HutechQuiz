using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FrontEnd.DTOs;

namespace frontend_manage.Pages.Login;

public class InfoApi
{
    private readonly HttpClient _httpClient;
    public InfoApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<StudentInfoDto?> GetStudentInfoByCodeAsync(string studentCode)
    {
        if (string.IsNullOrEmpty(studentCode)) return null;
        return await _httpClient.GetFromJsonAsync<StudentInfoDto>($"api/student/by-code/{studentCode}");
    }
}