using System.Net.Http.Json;
using frontend_manage.DTOs;
using Microsoft.AspNetCore.Components.Forms;

namespace frontend_manage.Services.Admin;

public class AdminStudentService
{
    private readonly HttpClient _httpClient;
    
    public AdminStudentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<StudentInfoDto>> GetAllStudentsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<StudentInfoDto>>("/api/Student");
            return response ?? new List<StudentInfoDto>();
        }
        catch
        {
            return new List<StudentInfoDto>();
        }
    }

    public async Task<StudentInfoDto?> GetByStudentCodeAsync(string studentCode)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<StudentInfoDto>($"/api/Student/by-code/{studentCode}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<StudentImportResultDto?> ImportFromExcelAsync(IBrowserFile file, string examSessionSubjectCore)
    {
        try
        {
            using var formData = new MultipartFormDataContent();
            using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB max
            formData.Add(new StreamContent(stream), "file", file.Name);
            formData.Add(new StringContent(examSessionSubjectCore), "examSessionSubjectCore");

            var response = await _httpClient.PostAsync("/api/Student/import-excel", formData);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<StudentImportResponse>();
                return new StudentImportResultDto
                {
                    StudentsAdded = result?.studentsAdded ?? 0,
                    StudentExamSessionsAdded = result?.studentExamSessionsAdded ?? 0,
                    Message = result?.message ?? "Import thành công"
                };
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ActiveLoginAsync(string studentCode, bool isLogin)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/Student/active-login", new { StudentCode = studentCode, IsLogin = isLogin });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AddExtraMinutesAsync(string studentCode, int studentExamSessionId, int extraMinutes, string? reasonForExtra)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/Student/extra-minutes", new 
            { 
                StudentCode = studentCode, 
                StudentExamSessionId = studentExamSessionId, 
                ExtraMinutes = extraMinutes,
                ReasonForExtra = reasonForExtra
            });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<StudentGradeDto>> GetStudentGradesAsync(int examSessionSubjectId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<StudentGradesResponse>($"/api/Student/grades/{examSessionSubjectId}");
            return response?.data ?? new List<StudentGradeDto>();
        }
        catch
        {
            return new List<StudentGradeDto>();
        }
    }

    public async Task<byte[]?> ExportStudentGradesAsync(int examSessionSubjectId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/Student/grades/{examSessionSubjectId}/export");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}

public class StudentImportResultDto
{
    public int StudentsAdded { get; set; }
    public int StudentExamSessionsAdded { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class StudentImportResponse
{
    public int studentsAdded { get; set; }
    public int studentExamSessionsAdded { get; set; }
    public string message { get; set; } = string.Empty;
}

public class StudentGradeDto
{
    public string StudentCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public double Score { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public bool IsCompleted { get; set; }
}

public class StudentGradesResponse
{
    public bool success { get; set; }
    public List<StudentGradeDto> data { get; set; } = new();
    public string subjectCode { get; set; } = string.Empty;
    public int count { get; set; }
}

