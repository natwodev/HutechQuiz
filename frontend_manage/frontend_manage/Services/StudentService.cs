using System.Net.Http.Json;
using System.Linq;
using frontend_manage.DTOs;
using frontend_manage.DTOs.Mapp;

namespace frontend_manage.Services;

public class StudentService
{
    private readonly HttpClient _httpClient;
    public StudentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ExamSubmissionDto?> GetSubmissionResultAsync(int studentExamSessionId)
    {
        try
        {
            var payload = new { StudentExamSessionId = studentExamSessionId };
            var response = await _httpClient.PostAsJsonAsync("/api/Student/get-submission-result", payload);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ExamSubmissionDto>();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return null;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<StudentInfoDto?> GetStudentProfileAsync()
    {
        return await _httpClient.GetFromJsonAsync<StudentInfoDto>("api/student/profile");
    }

    public async Task<List<StudentExamSessionDto>?> GetStudentExamSessionsAsync()
    {
        var allExamSessions = await _httpClient.GetFromJsonAsync<List<StudentExamSessionDto>>("api/Student/exam-sessions");
        
        return allExamSessions ?? new List<StudentExamSessionDto>();
    }

    public async Task<bool?> GetExamSessionSubjectIsOpenAsync(int examSessionSubjectId)
    {
        try
        {
            // Backend trả về boolean thuần
            var result = await _httpClient.GetFromJsonAsync<bool>($"api/ExamSessionSubject/{examSessionSubjectId}/is-open");
            return result;
        }
        catch
        {
            return null;
        }
    }

    public async Task<StartExamResponseDto?> StartExamAsync(int studentExamSessionId)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(studentExamSessionId.ToString()), "studentExamSessionId");
        var response = await _httpClient.PostAsync("api/Student/start-exam", form);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<StartExamResponseDto>();
            ApplyQuestionMapping(result);
            return result;
        }
        return null;
    }

    public async Task<StartExamResponseDto?> StartExamTestAsync()
    {
        var response = await _httpClient.PostAsync("api/Student/start-exam/test", null);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<StartExamResponseDto>();
            ApplyQuestionMapping(result);
            return result;
        }

        return null;
    }


    public async Task<SaveAnswerResponse?> SaveAnswerAsync(SaveAnswerDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/Student/save-answer", request);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = await response.Content.ReadFromJsonAsync<SaveAnswerResponse>();
                return result;
            }
            else
            {
                // Xử lý các status codes cụ thể
                var errorContent = await response.Content.ReadAsStringAsync();
                
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    // HTTP 429 - Rate limit exceeded
                    return new SaveAnswerResponse
                    {
                        Success = false,
                        Message = "Quá nhiều yêu cầu. Vui lòng thử lại sau vài giây.",
                        IsRateLimited = true,
                        RetryAfterSeconds = GetRetryAfterSeconds(response)
                    };
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return new SaveAnswerResponse
                    {
                        Success = false,
                        Message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
                        IsUnauthorized = true
                    };
                }
                else
                {
                    // Các lỗi khác
                    return new SaveAnswerResponse
                    {
                        Success = false,
                        Message = $"Lỗi server: {response.StatusCode}. {errorContent}",
                        StatusCode = (int)response.StatusCode
                    };
                }
            }
        }
        catch (Exception ex)
        {
            return new SaveAnswerResponse
            {
                Success = false,
                Message = $"Lỗi kết nối: {ex.Message}",
                IsConnectionError = true
            };
        }
    }

    public async Task<SubmitExamResponse?> SubmitExamAsync(SubmitExamRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/Student/submit-exam", request);
        
            var resultMessage = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        
            if (resultMessage != null && resultMessage.TryGetValue("message", out var msg))
            {
                return new SubmitExamResponse { Message = msg };
            }

            return new SubmitExamResponse { Message = "Không nhận được phản hồi từ server" };
        }
        catch (Exception ex)
        {
            return new SubmitExamResponse
            {
                Message = $"Lỗi kết nối khi nộp bài: {ex.Message}"
            };
        }
    }


    private int GetRetryAfterSeconds(HttpResponseMessage response)
    {
        // Kiểm tra header Retry-After nếu có
        if (response.Headers.RetryAfter?.Delta?.TotalSeconds > 0)
        {
            return (int)response.Headers.RetryAfter.Delta.Value.TotalSeconds;
        }
        
        // Mặc định 5 giây nếu không có header
        return 5;
    }

    private static void ApplyQuestionMapping(StartExamResponseDto? response)
    {
        if (response?.OriginalExamPaper?.Details == null || response.ExamPaper == null)
        {
            return;
        }

        // Nếu đã có QuestionStructures từ backend (đã hoán vị), hãy bổ sung nội dung từ OriginalExamPaper.Details
        if (response.ExamPaper.QuestionStructures != null && response.ExamPaper.QuestionStructures.Count > 0)
        {
            QuestionMapping.EnrichQuestionStructuresWithContent(response.ExamPaper.QuestionStructures, response.OriginalExamPaper.Details);
        }
        else
        {
            // Fallback nếu backend không trả về QuestionStructures
            var mappedQuestions = QuestionMapping.MapToQuestionStructureList(response.OriginalExamPaper.Details);
            response.ExamPaper.QuestionStructures = mappedQuestions;
        }
    }
}
