using System.Net.Http.Json;
using frontend_manage.DTOs;
using System.Linq;

namespace frontend_manage.Services;

public class StudentService
{
    private readonly HttpClient _httpClient;
    public StudentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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

    public async Task<StartExamResponseDto?> StartExamAsync(int studentExamSessionId)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(studentExamSessionId.ToString()), "studentExamSessionId");
        var response = await _httpClient.PostAsync("api/Student/start-exam", form);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<StartExamResponseDto>();
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
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SubmitExamResponse>();
                return result;
            }
            else
            {
                // Xử lý các status codes cụ thể
                var errorContent = await response.Content.ReadAsStringAsync();
                
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    // HTTP 429 - Rate limit exceeded - KHÔNG được chuyển trang kết quả
                    return new SubmitExamResponse
                    {
                        Success = false,
                        Message = "Quá nhiều yêu cầu nộp bài. Vui lòng thử lại sau vài giây.",
                        IsRateLimited = true,
                        RetryAfterSeconds = GetRetryAfterSeconds(response)
                    };
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return new SubmitExamResponse
                    {
                        Success = false,
                        Message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
                        IsUnauthorized = true
                    };
                }
                else
                {
                    // Các lỗi khác
                    return new SubmitExamResponse
                    {
                        Success = false,
                        Message = $"Lỗi server khi nộp bài: {response.StatusCode}. {errorContent}",
                        StatusCode = (int)response.StatusCode
                    };
                }
            }
        }
        catch (Exception ex)
        {
            return new SubmitExamResponse
            {
                Success = false,
                Message = $"Lỗi kết nối khi nộp bài: {ex.Message}",
                IsConnectionError = true
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
    
    public async Task<ExamSubmissionDto?> GetSubmissionResultAsync(GetSubmissionResultRequest request)
    {
        try
        {
            Console.WriteLine($"Bắt đầu gọi API get-submission-result với StudentExamSessionId: {request.StudentExamSessionId}");
            
            var response = await _httpClient.PostAsJsonAsync("/api/Student/get-submission-result", request);
            Console.WriteLine($"Kết quả gọi API: StatusCode={response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Nội dung phản hồi: {responseContent}");
                    
                    // Tạo lại nội dung để đọc lần nữa
                    var contentCopy = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json");
                    response = new HttpResponseMessage(response.StatusCode) { Content = contentCopy };
                    
                    var result = await response.Content.ReadFromJsonAsync<ExamSubmissionDto>();
                    Console.WriteLine($"Parse JSON thành công: {(result != null ? "Có" : "Không")}. StudentCode: {result?.StudentCode}");
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi xử lý phản hồi: {ex.Message}");
                    return null;
                }
            }
            else
            {
                // Xử lý lỗi và trả về null
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = $"Lỗi khi lấy kết quả nộp bài: {response.StatusCode}. {errorContent}";
                Console.WriteLine(errorMessage);
                
                // Tạo một đối tượng với thông tin lỗi để hiển thị
                return new ExamSubmissionDto
                {
                    StudentCode = "ERROR",
                    ShuffledExamPaperId = 0,
                    StudentAnswersString = errorMessage,
                    AnswerKey = $"StatusCode: {(int)response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi kết nối khi lấy kết quả nộp bài: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return null;
        }
    }
}
