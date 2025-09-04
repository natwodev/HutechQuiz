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
        private SubmitExamData? result;
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

                // Dữ liệu đã được kiểm tra trong ValidateAccessAsync
                var sessionIdStr = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "currentStudentExamSessionId");
                var examResultJson = await JSRuntime.InvokeAsync<string>("localStorage.getItem", $"examResult_{sessionIdStr}");

                result = ParseExamResult(examResultJson);
                if (result == null)
                {
                    SetErrorAndRedirect("Không thể đọc dữ liệu kết quả bài thi.");
                    return;
                }

                BuildHeatmap();

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

        // ===================== API Integration (DÙNG DỮ LIỆU THẬT) =====================
        // Khi sử dụng API thật, hệ thống sẽ:
        // 1. Kiểm tra quyền truy cập từ localStorage (currentStudentExamSessionId)
        // 2. Lấy dữ liệu kết quả bài thi từ localStorage (examResult_{sessionId})
        // 3. Parse JSON thành SubmitExamData
        // 4. Xây dựng heatmap từ StudentAnswersString và AnswerKey để hiển thị trạng thái từng câu
        // 5. Sử dụng trực tiếp điểm số và số câu đúng từ API
        // ===================== Access Validation =====================
        private async Task<bool> ValidateAccessAsync()
        {
            try
            {
                var sessionIdStr = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "currentStudentExamSessionId");

                if (string.IsNullOrEmpty(sessionIdStr))
                {
                    SetErrorAndRedirect("Truy cập không hợp lệ. Vui lòng làm bài thi trước.");
                    return false;
                }

                // Kiểm tra xem có dữ liệu kết quả tương ứng không
                var examResultJson = await JSRuntime.InvokeAsync<string>("localStorage.getItem", $"examResult_{sessionIdStr}");
                if (string.IsNullOrEmpty(examResultJson))
                {
                    SetErrorAndRedirect("Không tìm thấy dữ liệu kết quả bài thi.");
                    return false;
                }

                return true;
            }
            catch
            {
                SetErrorAndRedirect("Lỗi khi kiểm tra quyền truy cập.");
                return false;
            }
        }



        // ===================== Data Parsing =====================
        private SubmitExamData? ParseExamResult(string json)
        {
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
                if (jsonElement.ValueKind == JsonValueKind.Object)
                {
                    return new SubmitExamData
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



        // ===================== Heatmap Building (API THẬT) =====================
        // Logic xây dựng heatmap với dữ liệu thật:
        // - Ưu tiên sử dụng StudentAnswersString và AnswerKey từ API để hiển thị trạng thái chi tiết từng câu
        // - Fallback về CreateHeatmapFromApiData nếu không có dữ liệu chi tiết
        // - Heatmap chỉ dùng để hiển thị trạng thái, không dùng để tính toán điểm số
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
            // Tạo heatmap chi tiết từ dữ liệu thực tế của sinh viên và đáp án
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

            var totalQuestions = result.TotalQuestions ?? 0;
            for (int displayOrder = 1; displayOrder <= totalQuestions; displayOrder++)
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
            // Fallback: Tạo heatmap đơn giản dựa trên số câu đúng từ API
            var correctCount = result!.CorrectAnswers ?? 0;
            var totalQuestions = result!.TotalQuestions ?? 0;

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
            // Parse chuỗi đáp án từ API thành Dictionary để xây dựng heatmap
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
        // Quy tắc xác định trạng thái câu hỏi cho heatmap:
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

        // Chuẩn hoá đáp án để so sánh công bằng khi xây dựng heatmap
        // Hỗ trợ cả OptionId số & A/B/C/D, multi-select
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
        // Sử dụng trực tiếp dữ liệu từ API, không tính toán lại

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