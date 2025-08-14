using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Net.Http.Json;
using frontend_manage.DTOs;

namespace frontend_manage.Pages.Exam;

public partial class Result : ComponentBase
{
    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;

    [Inject]
    protected ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    protected HttpClient Http { get; set; } = default!;

    [Inject]
    protected IJSRuntime JSRuntime { get; set; } = default!;

    protected bool loading = true;
    protected SubmitExamResponse? examResult;
    protected List<QuestionAnswer> questionAnswers = new();
    protected int studentExamSessionId;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Lấy studentExamSessionId từ localStorage hoặc session storage
            var sessionIdFromStorage = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "currentStudentExamSessionId");
            
            if (!string.IsNullOrEmpty(sessionIdFromStorage) && int.TryParse(sessionIdFromStorage, out var sessionId))
            {
                studentExamSessionId = sessionId;
            }
            else
            {
                // Nếu không có trong localStorage, thử lấy từ sessionStorage
                sessionIdFromStorage = await JSRuntime.InvokeAsync<string>("sessionStorage.getItem", "currentStudentExamSessionId");
                if (!string.IsNullOrEmpty(sessionIdFromStorage) && int.TryParse(sessionIdFromStorage, out sessionId))
                {
                    studentExamSessionId = sessionId;
                }
                else
                {
                    // Nếu không tìm thấy studentExamSessionId, redirect về trang chủ hoặc hiển thị lỗi
                    Snackbar.Add("Không tìm thấy thông tin bài thi. Vui lòng thử lại!", Severity.Error);
                    Navigation.NavigateTo("/");
                    return;
                }
            }

            // Thử đọc dữ liệu từ localStorage trước
            var resultJson = await JSRuntime.InvokeAsync<string>("localStorage.getItem", $"examResult_{studentExamSessionId}");
            
            if (!string.IsNullOrEmpty(resultJson))
            {
                try
                {
                    // Parse dữ liệu từ localStorage
                    var resultData = System.Text.Json.JsonSerializer.Deserialize<ExamResultData>(resultJson);
                    if (resultData != null)
                    {
                        examResult = new SubmitExamResponse
                        {
                            Success = true,
                            Message = "Kết quả bài thi",
                            Data = new SubmitExamData
                            {
                                StudentCode = resultData.StudentCode ?? "",
                                ShuffledExamPaperId = resultData.ShuffledExamPaperId,
                                Score = resultData.Score,
                                CorrectAnswers = resultData.CorrectAnswers,
                                TotalQuestions = resultData.TotalQuestions,
                                StartTime = resultData.StartTime,
                                EndTime = resultData.EndTime,
                                StudentAnswersString = resultData.StudentAnswersString ?? "",
                                AnswerKey = resultData.AnswerKey ?? ""
                            }
                        };

                        ParseAnswers(examResult.Data.StudentAnswersString, examResult.Data.AnswerKey);
                        
                        // Xóa dữ liệu khỏi localStorage sau khi đã sử dụng
                        await JSRuntime.InvokeVoidAsync("localStorage.removeItem", $"examResult_{studentExamSessionId}");
                        await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "currentStudentExamSessionId");
                    }
                }
                catch (Exception ex)
                {
                    // Nếu parse lỗi, gọi API
                    await LoadFromApiAsync();
                }
            }
            else
            {
                // Nếu không có dữ liệu trong localStorage, gọi API
                await LoadFromApiAsync();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Không thể tải kết quả bài thi: {ex.Message}", Severity.Error);
        }
        finally
        {
            loading = false;
        }
    }

    private async Task LoadFromApiAsync()
    {
        try
        {
            // Gọi API submit-exam để lấy kết quả bài thi (fallback)
            var submitRequest = new SubmitExamRequest
            {
                StudentExamSessionId = studentExamSessionId
            };
            
            var response = await Http.PostAsJsonAsync("/api/Student/submit-exam", submitRequest);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SubmitExamResponse>();
                if (result != null && result.Data != null)
                {
                    examResult = result;
                    ParseAnswers(examResult.Data.StudentAnswersString, examResult.Data.AnswerKey);
                }
                else
                {
                    Snackbar.Add("Không tìm thấy kết quả bài thi", Severity.Warning);
                }
            }
            else
            {
                Snackbar.Add($"Lỗi khi tải kết quả bài thi: {response.StatusCode} - {responseContent}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Không thể tải kết quả bài thi từ API: {ex.Message}", Severity.Error);
        }
    }

    private void ParseAnswers(string? studentAnswersString, string? answerKey)
    {
        questionAnswers.Clear();
        if (string.IsNullOrEmpty(studentAnswersString) || string.IsNullOrEmpty(answerKey))
        {
            return;
        }

        try
        {
            // Remove outer parentheses and split by semicolon
            var studentAnswers = studentAnswersString.Trim('(', ')').Split(';', StringSplitOptions.RemoveEmptyEntries);
            var correctAnswers = answerKey.Trim('(', ')').Split(';', StringSplitOptions.RemoveEmptyEntries);

            // Parse student answers
            var answerDict = new Dictionary<string, string>();
            foreach (var answer in studentAnswers)
            {
                var trimmedAnswer = answer.Trim('(', ')');
                var parts = trimmedAnswer.Split(',');
                if (parts.Length == 2)
                {
                    var questionNumber = parts[0].Trim();
                    var studentAnswer = parts[1].Trim();
                    answerDict[questionNumber] = studentAnswer;
                }
            }

            // Parse correct answers and create question answers
            foreach (var correctAnswer in correctAnswers)
            {
                var trimmedAnswer = correctAnswer.Trim('(', ')');
                var parts = trimmedAnswer.Split(',');
                if (parts.Length == 2)
                {
                    var questionNumber = parts[0].Trim();
                    var correctAnswerValue = parts[1].Trim();
                    
                    answerDict.TryGetValue(questionNumber, out var studentAnswer);
                    var isCorrect = !string.IsNullOrEmpty(studentAnswer) && studentAnswer == correctAnswerValue;
                    
                    var questionAnswer = new QuestionAnswer
                    {
                        QuestionNumber = questionNumber,
                        StudentAnswer = studentAnswer ?? "-",
                        CorrectAnswer = correctAnswerValue,
                        IsCorrect = isCorrect
                    };
                    
                    questionAnswers.Add(questionAnswer);
                }
            }
        }
        catch (Exception ex)
        {
            // Error parsing answers
        }
    }

    protected Color GetAnswerColor(QuestionAnswer q)
    {
        if (q.StudentAnswer == "-") return Color.Default;
        return q.IsCorrect ? Color.Success : Color.Error;
    }

    protected string GetAnswerIcon(QuestionAnswer q)
    {
        if (q.StudentAnswer == "-") return Icons.Material.Filled.Remove;
        return q.IsCorrect ? Icons.Material.Filled.Check : Icons.Material.Filled.Close;
    }

    protected string GetAnswerText(QuestionAnswer q)
    {
        if (q.StudentAnswer == "-") return "-";
        return q.StudentAnswer;
    }

    private string GetAnswerTooltip(QuestionAnswer q)
    {
        if (q.StudentAnswer == "-") return "Không trả lời";
        if (q.IsCorrect) return $"Đúng! Đáp án: {q.CorrectAnswer}";
        return $"Sai! Đáp án đúng: {q.CorrectAnswer}";
    }

    private string GetAnswerStyle(QuestionAnswer q)
    {
        if (q.StudentAnswer == "-") 
            return "background-color: #f5f5f5; color: #666; border: 1px solid #ddd;"; // Không trả lời - xám nhạt
        if (q.IsCorrect) 
            return "background-color: #4caf50; color: white; border: 1px solid #4caf50;"; // Đúng - xanh lá
        return "background-color: #f44336; color: white; border: 1px solid #f44336;"; // Sai - đỏ
    }

    private string GetAnswerClass(QuestionAnswer q)
    {
        if (q.StudentAnswer == "-") return "answer-not-answered";
        if (q.IsCorrect) return "answer-correct";
        return "answer-incorrect";
    }

    protected void GoToStudentDashboard() => Navigation.NavigateTo("/student-dashboard");

    public class QuestionAnswer
    {
        public string QuestionNumber { get; set; } = "";
        public string StudentAnswer { get; set; } = "";
        public string CorrectAnswer { get; set; } = "";
        public bool IsCorrect { get; set; } = false;
    }

    // Class để deserialize dữ liệu từ localStorage
    private class ExamResultData
    {
        public string? StudentCode { get; set; }
        public int ShuffledExamPaperId { get; set; }
        public double? Score { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalQuestions { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? StudentAnswersString { get; set; }
        public string? AnswerKey { get; set; }
    }
}