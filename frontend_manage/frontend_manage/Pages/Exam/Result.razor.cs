using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Text.Json;
using frontend_manage.DTOs;

namespace frontend_manage.Pages.Exam
{
    public partial class Result : ComponentBase
    {
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private NavigationManager Navigation { get; set; } = default!;

        // ===================== State =====================
        private ExamResultDataDto? result;
        private bool isLoading = true;
        private string errorMessage = string.Empty;
        private List<HeatCell> heatCells = new();
        private int rows = 7;
        private int cols = 1;

        // ===================== Lifecycle =====================
        protected override async Task OnInitializedAsync()
        {
            await LoadExamResultAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    // Cần gọi 1 hàm JS có thật; dùng eval cho nhanh (khuyến nghị sau này đưa vào file .js riêng)
                    await JSRuntime.InvokeVoidAsync("eval", @"
                        window.addEventListener('popstate', function() {
                            localStorage.clear();
                            window.location.assign('/student-login');
                        });
                        history.pushState(null, '', window.location.href);
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in OnAfterRenderAsync: {ex.Message}");
                }
            }
        }

        // ===================== Main Logic =====================
        private async Task LoadExamResultAsync()
        {
            try
            {
                isLoading = true;
                errorMessage = string.Empty;

                // ===== DÙNG DỮ LIỆU THẬT =====
                if (!await ValidateAccessAsync()) return;
                var examResultJson = await GetExamResultJsonAsync();
                if (string.IsNullOrEmpty(examResultJson))
                {
                    SetErrorAndRedirect("Không tìm thấy dữ liệu kết quả bài thi.");
                    return;
                }
                result = ParseExamResult(examResultJson);
                if (result == null)
                {
                    SetErrorAndRedirect("Không thể đọc dữ liệu kết quả bài thi.");
                    return;
                }
                // Lưu ý: Khi dùng dữ liệu thật, điểm số sẽ được tính toán lại từ heatmap
                // để đảm bảo tính nhất quán với hiển thị

                BuildHeatmap();
                
                // Tính toán điểm số dựa trên kết quả thực tế từ heatmap
                CalculateScoreFromHeatmap();

                // Xóa dữ liệu sau 5 phút để bảo mật
                _ = Task.Run(async () =>
                {
                    await Task.Delay(300000); // 5 phút
                    await InvokeAsync(async () =>
                    {
                        try
                        {
                            await JSRuntime.InvokeVoidAsync("eval", "localStorage.clear()");
                        }
                        catch { /* Ignore */ }
                    });
                });
            }
            catch (Exception ex)
            {
                errorMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        // ===================== Mock Data for Testing (ĐÃ COMMENT LẠI) =====================
        // Test case: 50 câu hỏi với kết quả chính xác:
        // - 20 câu đầu: Sinh viên trả lời "A", đáp án "A" → ĐÚNG
        // - 20 câu tiếp: Sinh viên trả lời "B", đáp án "C" → SAI  
        // - 10 câu cuối: Sinh viên bỏ trống "-", đáp án "-" → CHƯA TRẢ LỜI
        /*
        private ExamResultDataDto CreateMockExamResult()
        {
            var mockStudentAnswers = GenerateMockStudentAnswers(50); // Giảm xuống 50 câu để dễ test
            var mockAnswerKey = GenerateMockAnswerKey(50);
            var actualCorrectAnswers = CalculateActualCorrectAnswers(mockStudentAnswers, mockAnswerKey);

            return new ExamResultDataDto
            {
                StudentCode = "SV001",
                ShuffledExamPaperId = 12345,
                Score = 0.0, // Sẽ được tính toán sau khi xây dựng heatmap
                CorrectAnswers = actualCorrectAnswers,
                TotalQuestions = 50, // Giảm xuống 50 câu
                StartTime = DateTime.Now.AddHours(-2),
                EndTime = DateTime.Now.AddMinutes(-30),
                StudentAnswersString = mockStudentAnswers,
                AnswerKey = mockAnswerKey
            };
        }
        */

        /*
        private int CalculateActualCorrectAnswers(string studentAnswersString, string answerKeyString)
        {
            var studentAnswers = ParseAnswerString(studentAnswersString);
            var correctAnswers = ParseAnswerString(answerKeyString);

            int correctCount = 0;

            foreach (var kvp in studentAnswers)
            {
                var qid = kvp.Key;
                var student = NormalizeAnswer(kvp.Value);
                var correct = NormalizeAnswer(correctAnswers.GetValueOrDefault(qid, ""));

                // Chỉ tính câu đúng khi:
                // 1. Sinh viên đã trả lời (không bỏ trống)
                // 2. Có đáp án đúng để so sánh
                if (string.IsNullOrEmpty(student)) continue; // SV bỏ trống
                if (string.IsNullOrEmpty(correct)) continue; // Không có đáp án

                if (student == correct) correctCount++;
            }

            return correctCount;
        }

        private string GenerateMockStudentAnswers(int totalQuestions)
        {
            var answers = new List<string>();
            var random = new Random(42); // seed cố định

            // Tạo chính xác: 20 câu đúng, 20 câu sai, 10 câu chưa trả lời
            for (int i = 1; i <= totalQuestions; i++)
            {
                var questionNo = i + 19; // 20.. (giống DB thực)
                
                string answer;
                if (i <= 20)
                {
                    // 20 câu đầu: sinh viên trả lời (sẽ đúng)
                    answer = "A"; // Cố định để dễ test
                }
                else if (i <= 40)
                {
                    // 20 câu tiếp: sinh viên trả lời (sẽ sai)
                    answer = "B"; // Cố định để dễ test
                }
                else
                {
                    // 10 câu cuối: sinh viên bỏ trống
                    answer = "-";
                }

                answers.Add($"({questionNo}:{answer})");
            }

            return string.Join(";", answers);
        }

        private string GenerateMockAnswerKey(int totalQuestions)
        {
            var answers = new List<string>();
            var random = new Random(123);

            // Tạo đáp án để test case: 20 câu đúng, 20 câu sai, 10 câu chưa trả lời
            for (int i = 1; i <= totalQuestions; i++)
            {
                var qid = i + 19;
                
                string answer;
                if (i <= 20)
                {
                    // 20 câu đầu: đáp án trùng với sinh viên (sẽ đúng)
                    answer = "A"; // Trùng với sinh viên
                }
                else if (i <= 40)
                {
                    // 20 câu tiếp: đáp án khác với sinh viên (sẽ sai)
                    answer = "C"; // Khác với sinh viên (B)
                }
                else
                {
                    // 10 câu cuối: không có đáp án (câu chưa trả lời)
                    answer = "-";
                }

                answers.Add($"({questionNo}:{answer})");
            }

            return string.Join(";", answers);
        }
        */

        // ===================== API Integration (DÙNG DỮ LIỆU THẬT) =====================
        // Khi sử dụng API thật, hệ thống sẽ:
        // 1. Kiểm tra quyền truy cập từ localStorage
        // 2. Lấy dữ liệu kết quả bài thi từ localStorage
        // 3. Parse JSON thành ExamResultDataDto
        // 4. Xây dựng heatmap từ StudentAnswersString và AnswerKey
        // 5. Tính toán lại điểm số dựa trên kết quả thực tế
        // ===================== Access Validation =====================
        private async Task<bool> ValidateAccessAsync()
        {
            try
            {
                var examCompletedFlag = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "examCompletedFlag");
                var examSubmitTimeStr = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "examSubmitTime");
                var sessionIdStr = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "currentStudentExamSessionId");

                if (string.IsNullOrEmpty(examCompletedFlag) ||
                    string.IsNullOrEmpty(examSubmitTimeStr) ||
                    string.IsNullOrEmpty(sessionIdStr))
                {
                    SetErrorAndRedirect("Truy cập không hợp lệ. Vui lòng làm bài thi trước.");
                    return false;
                }

                if (DateTime.TryParse(examSubmitTimeStr, out var examSubmitTime))
                {
                    var timeDiff = DateTime.Now - examSubmitTime;
                    if (timeDiff.TotalMinutes > 30)
                    {
                        SetErrorAndRedirect("Phiên xem kết quả đã hết hạn. Vui lòng liên hệ giảng viên.");
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                SetErrorAndRedirect("Lỗi khi kiểm tra quyền truy cập.");
                return false;
            }
        }

        private async Task<string?> GetExamResultJsonAsync()
        {
            try
            {
                var sessionIdStr = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "currentStudentExamSessionId");
                if (int.TryParse(sessionIdStr, out int sessionId))
                {
                    return await JSRuntime.InvokeAsync<string>("localStorage.getItem", $"examResult_{sessionId}");
                }
            }
            catch { /* Ignore */ }
            return null;
        }

        // ===================== Data Parsing =====================
        private ExamResultDataDto? ParseExamResult(string json)
        {
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
                if (jsonElement.ValueKind == JsonValueKind.Object)
                {
                    return new ExamResultDataDto
                    {
                        StudentCode = jsonElement.GetProperty("StudentCode").GetString() ?? "",
                        ShuffledExamPaperId = jsonElement.GetProperty("ShuffledExamPaperId").GetInt32(),
                        Score = jsonElement.GetProperty("Score").GetDouble(),
                        CorrectAnswers = jsonElement.GetProperty("CorrectAnswers").GetInt32(),
                        TotalQuestions = jsonElement.GetProperty("TotalQuestions").GetInt32(),
                        StartTime = jsonElement.GetProperty("StartTime").GetDateTime(),
                        EndTime = jsonElement.GetProperty("EndTime").GetDateTime(),
                        StudentAnswersString = jsonElement.GetProperty("StudentAnswersString").GetString() ?? "",
                        AnswerKey = jsonElement.GetProperty("AnswerKey").GetString() ?? ""
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing exam result: {ex.Message}");
            }
            return null;
        }

        // ===================== Score Calculation (API THẬT) =====================
        // Logic tính điểm với dữ liệu thật:
        // - Điểm số từ API có thể không chính xác hoặc cũ
        // - Hệ thống sẽ tính toán lại dựa trên heatmap thực tế
        // - Đảm bảo tính nhất quán giữa hiển thị và dữ liệu
        // - Hỗ trợ cả trường hợp câu chưa trả lời
        private void CalculateScoreFromHeatmap()
        {
            if (result == null || heatCells?.Count == 0) return;

            var totalQuestions = result.TotalQuestions;
            var correctAnswers = heatCells.Count(c => c.StatusCss == "ok");
            var wrongAnswers = heatCells.Count(c => c.StatusCss == "bad");
            var emptyAnswers = heatCells.Count(c => c.StatusCss == "empty");

            // Cập nhật số câu đúng thực tế
            result.CorrectAnswers = correctAnswers;

            // Tính điểm: 10 điểm cho 100% câu đúng
            var score = totalQuestions > 0 ? (double)correctAnswers / totalQuestions * 10.0 : 0.0;
            result.Score = Math.Round(score, 2);

            Console.WriteLine($"CalculateScoreFromHeatmap - Total: {totalQuestions}, Correct: {correctAnswers}, Wrong: {wrongAnswers}, Empty: {emptyAnswers}");
            Console.WriteLine($"CalculateScoreFromHeatmap - Calculated Score: {result.Score}");
            
            // Log để debug
            Console.WriteLine($"CalculateScoreFromHeatmap - Final result.Score: {result.Score}, result.CorrectAnswers: {result.CorrectAnswers}");
            
            // Log test case results
            Console.WriteLine("=== TEST CASE RESULTS ===");
            Console.WriteLine($"Expected: 20 correct, 20 wrong, 10 empty");
            Console.WriteLine($"Actual: {correctAnswers} correct, {wrongAnswers} wrong, {emptyAnswers} empty");
            Console.WriteLine($"Score: {result.Score}/10.0 ({(result.Score/10.0*100):0.0}%)");
            Console.WriteLine("========================");
        }

        // ===================== Heatmap Building (API THẬT) =====================
        // Logic xây dựng heatmap với dữ liệu thật:
        // - Ưu tiên sử dụng StudentAnswersString và AnswerKey từ API
        // - Fallback về CreateHeatmapFromApiData nếu không có dữ liệu chi tiết
        // - Đảm bảo hiển thị chính xác trạng thái từng câu hỏi
        private void BuildHeatmap()
        {
            if (result == null) return;

            heatCells.Clear();

            if (!string.IsNullOrWhiteSpace(result.StudentAnswersString) &&
                !string.IsNullOrWhiteSpace(result.AnswerKey))
            {
                CreateHeatmapFromRealData();
            }
            else
            {
                CreateHeatmapFromApiData();
            }
        }

        private void CreateHeatmapFromRealData()
        {
            var studentAnswers = ParseAnswerString(result!.StudentAnswersString);
            var correctAnswers = ParseAnswerString(result!.AnswerKey);

            Console.WriteLine($"CreateHeatmapFromRealData - StudentAnswersString: '{result.StudentAnswersString}'");
            Console.WriteLine($"CreateHeatmapFromRealData - AnswerKey: '{result.AnswerKey}'");
            Console.WriteLine($"CreateHeatmapFromRealData - studentAnswers.Count: {studentAnswers.Count}");
            Console.WriteLine($"CreateHeatmapFromRealData - correctAnswers.Count: {correctAnswers.Count}");

            // Nếu cả hai không parse được gì -> fallback API
            if (studentAnswers.Count == 0 && correctAnswers.Count == 0)
            {
                Console.WriteLine("Fallback to CreateHeatmapFromApiData because parsing returned 0.");
                CreateHeatmapFromApiData();
                return;
            }

            // Lấy union các QuestionId thực tế
            var qids = studentAnswers.Keys.Union(correctAnswers.Keys).OrderBy(x => x).ToList();
            Console.WriteLine($"CreateHeatmapFromRealData - All QIDs: {string.Join(", ", qids)}");

            // Map thứ tự hiển thị 1..TotalQuestions sang QID (nếu thiếu QID, ô sẽ là empty)
            var questionMapping = new Dictionary<int, int>();
            for (int i = 0; i < qids.Count; i++)
            {
                questionMapping[i + 1] = qids[i];
            }

            for (int displayOrder = 1; displayOrder <= result.TotalQuestions; displayOrder++)
            {
                if (questionMapping.TryGetValue(displayOrder, out var qid))
                {
                    var studentRaw = studentAnswers.GetValueOrDefault(qid, "");
                    var correctRaw = correctAnswers.GetValueOrDefault(qid, "");

                    var statusCss = GetStatusCss(studentRaw, correctRaw);
                    var statusText = GetStatusText(studentRaw, correctRaw);

                    Console.WriteLine($"Display {displayOrder} -> Q{qid}: Student='{studentRaw}', Correct='{correctRaw}' -> {statusText} ({statusCss})");

                    heatCells.Add(new HeatCell
                    {
                        QuestionNo = displayOrder,
                        StatusCss = statusCss,
                        StatusText = statusText
                    });
                }
                else
                {
                    // Không có QID tương ứng cho vị trí này
                    heatCells.Add(new HeatCell
                    {
                        QuestionNo = displayOrder,
                        StatusCss = "empty",
                        StatusText = "Chưa trả lời"
                    });
                }
            }

            cols = (int)Math.Ceiling(heatCells.Count / (double)rows);

            Console.WriteLine($"CreateHeatmapFromRealData - Final heatCells.Count: {heatCells.Count}");
            var okCount = heatCells.Count(c => c.StatusCss == "ok");
            var badCount = heatCells.Count(c => c.StatusCss == "bad");
            var emptyCount = heatCells.Count(c => c.StatusCss == "empty");
            Console.WriteLine($"CreateHeatmapFromRealData - Final counts: OK={okCount}, BAD={badCount}, EMPTY={emptyCount}");

            StateHasChanged();
        }

        private void CreateHeatmapFromApiData()
        {
            var correctCount = result!.CorrectAnswers;
            var totalQuestions = result!.TotalQuestions;

            for (int i = 1; i <= totalQuestions; i++)
            {
                heatCells.Add(new HeatCell
                {
                    QuestionNo = i,
                    StatusCss = i <= correctCount ? "ok" : "bad",
                    StatusText = i <= correctCount ? "Đúng" : "Sai"
                });
            }

            cols = (int)Math.Ceiling(totalQuestions / (double)rows);
            StateHasChanged();
        }

        private Dictionary<int, string> ParseAnswerString(string answerString)
        {
            var dict = new Dictionary<int, string>();
            if (string.IsNullOrWhiteSpace(answerString))
            {
                Console.WriteLine($"ParseAnswerString - Empty or null string: '{answerString}'");
                return dict;
            }

            Console.WriteLine($"ParseAnswerString - Input: '{answerString}'");
            var parts = answerString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine($"ParseAnswerString - Parts count: {parts.Length}");

            foreach (var part in parts)
            {
                Console.WriteLine($"ParseAnswerString - Processing part: '{part}'");
                var match = Regex.Match(part, @"\((\d+):([^)]+)\)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int qid))
                {
                    var ans = match.Groups[2].Value.Trim();
                    // GIỮ nguyên "-" tại đây, sẽ normalize sau khi chấm/so sánh
                    dict[qid] = ans;
                    Console.WriteLine($"ParseAnswerString - Parsed: Q{qid} = '{dict[qid]}'");
                }
                else
                {
                    Console.WriteLine($"ParseAnswerString - Failed to parse part: '{part}'");
                }
            }

            Console.WriteLine($"ParseAnswerString - Final result: {string.Join(", ", dict.Select(kv => $"Q{kv.Key}:{kv.Value}"))}");
            return dict;
        }

        // ============= So sánh / Hiển thị trạng thái =============
        // Quy tắc:
        // - Chỉ 'ok' khi CẢ HAI đều không rỗng và bằng nhau sau Normalize
        // - Nếu student là "-" (bỏ trống), coi là chưa trả lời -> "empty"
        // - Nếu correct là "-" (không có đáp án), coi là "bad" (sai)
        // - Các trường hợp còn lại -> "bad" (sai)
        // 
        // Ví dụ với backend trả về: (28:-);(29:-)
        // - Câu 28: student="-", correct="A" -> "empty" (chưa trả lời)
        // - Câu 29: student="-", correct="B" -> "empty" (chưa trả lời)
        private string GetStatusCss(string studentAnswer, string correctAnswer)
        {
            // Kiểm tra trực tiếp "-" trước khi normalize
            // Backend trả về: (28:-);(29:-) -> sinh viên bỏ trống câu 28, 29
            if (studentAnswer?.Trim() == "-") return "empty"; // SV bỏ trống
            
            var s = NormalizeAnswer(studentAnswer);
            var c = NormalizeAnswer(correctAnswer);

            if (string.IsNullOrEmpty(s)) return "empty"; // SV bỏ trống
            if (string.IsNullOrEmpty(c)) return "bad";   // key trống -> không thể coi là đúng

            return s == c ? "ok" : "bad";
        }

        private string GetStatusText(string studentAnswer, string correctAnswer)
        {
            // Kiểm tra trực tiếp "-" trước khi normalize
            if (studentAnswer?.Trim() == "-") return "Chưa trả lời";
            
            var s = NormalizeAnswer(studentAnswer);
            var c = NormalizeAnswer(correctAnswer);

            if (string.IsNullOrEmpty(s)) return "Chưa trả lời";
            if (string.IsNullOrEmpty(c)) return "Sai";
            return s == c ? "Đúng" : "Sai";
        }

        // Chuẩn hoá đáp án để so sánh công bằng, hỗ trợ cả OptionId số & A/B/C/D, multi-select.
        // Lưu ý: "-" được giữ nguyên để xử lý đặc biệt trong GetStatusCss/GetStatusText
        private static string NormalizeAnswer(string a)
        {
            if (string.IsNullOrWhiteSpace(a)) return "";
            var x = a.Trim();

            // Quy ước "-" là bỏ trống - giữ nguyên để xử lý đặc biệt
            if (x == "-") return "-";

            // Nếu có phân tách (multi-select): "A,C", "A|C", "A C", "A/C"
            var tokens = Regex.Split(x, @"[,\|/\s]+")
                              .Where(t => !string.IsNullOrWhiteSpace(t))
                              .Select(t => t.Trim().ToUpperInvariant())
                              .ToArray();

            if (tokens.Length == 0) return "";
            if (tokens.Length == 1) return tokens[0];

            Array.Sort(tokens, StringComparer.Ordinal);
            return string.Concat(tokens);
        }

        // ===================== Helper Methods =====================
        private void SetErrorAndRedirect(string message)
        {
            errorMessage = message;
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000);
                await InvokeAsync(async () =>
                {
                    try
                    {
                        await JSRuntime.InvokeVoidAsync("window.location.assign", "/student-login");
                    }
                    catch
                    {
                        Navigation.NavigateTo("/student-login");
                    }
                });
            });
        }

        // ===================== Computed Properties =====================
        private double ScorePercent => result?.TotalQuestions > 0
            ? (double)(result.CorrectAnswers) / result.TotalQuestions * 100.0
            : 0.0;

        private string ScoreSvgDataUri => BuildScoreSvgDataUri(
            result?.Score ?? 0,
            result?.CorrectAnswers ?? 0,
            result?.TotalQuestions ?? 0);

        // ===================== SVG Generation =====================
        private string BuildScoreSvgDataUri(double score, int correct, int total, int width = 560, int height = 220)
        {
            var scoreText = score.ToString("0.00");
            var midY = (int)Math.Round(height * 0.62);
            var bigFont = (int)Math.Round(height * 0.60);

            var svg = $@"
<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}'>
  <defs>
    <linearGradient id='g' x1='0' y1='0' x2='1' y2='1'>
      <stop offset='0' stop-color='#1976d2'/><stop offset='1' stop-color='#42a5f5'/>
    </linearGradient>
    <filter id='ds' x='-20%' y='-20%' width='140%' height='140%'>
      <feDropShadow dx='0' dy='8' stdDeviation='10' flood-color='#1565c0' flood-opacity='.35'/>
    </filter>
  </defs>
  <rect rx='24' width='{width}' height='{height}' fill='url(#g)' filter='url(#ds)'/>
  <text x='{width / 2}' y='{midY}' text-anchor='middle'
        font-family='Segoe UI,Roboto,Arial' font-weight='900' font-size='{bigFont}'
        fill='#ffffff'>{scoreText}</text>
  <text x='{width / 2}' y='{height - 18}' text-anchor='middle'
        font-family='Segoe UI,Roboto,Arial' font-size='18' fill='rgba(255,255,255,.9)'>
    {correct}/{total} câu đúng
  </text>
</svg>";

            var bytes = System.Text.Encoding.UTF8.GetBytes(svg);
            return "data:image/svg+xml;base64," + Convert.ToBase64String(bytes);
        }

        // ===================== Public Methods =====================
        public void Reload() => _ = LoadExamResultAsync();
    }

    // ===================== Models =====================
    public class HeatCell
    {
        public int QuestionNo { get; set; }
        public string StatusCss { get; set; } = "empty";
        public string StatusText { get; set; } = "Chưa trả lời";
    }
}
