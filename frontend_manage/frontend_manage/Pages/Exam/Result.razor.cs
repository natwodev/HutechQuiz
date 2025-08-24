using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Net.Http;

namespace frontend_manage.Pages.Exam
{
    // ===================== Models =====================
    public class ExamResultVm
    {
        public ExamResultDataVm Data { get; set; } = new();
    }

    public class ExamResultDataVm
    {
        public string StudentCode { get; set; } = "";
        public int ShuffledExamPaperId { get; set; }
        public double Score { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Đáp án SV theo định dạng: "(1:1);(2:-);(3:10)..."
        /// Trong đó giá trị là VỊ TRÍ SỐ; "-" nghĩa là chưa trả lời.
        /// </summary>
        public string StudentAnswersString { get; set; } = "";

        /// <summary>
        /// Đáp án đúng theo định dạng: "(1:1);(2:6);(3:10)..."
        /// (Giá trị là VỊ TRÍ SỐ.)
        /// </summary>
        public string AnswerKey { get; set; } = "";
    }

    public class AnswerDetail
    {
        public int QuestionNo { get; set; }
        public string StudentAnswer { get; set; } = "";  // "" = chưa trả lời
        public string CorrectAnswer { get; set; } = "";
    }

    public class HeatCell
    {
        public int QuestionNo { get; set; }
        /// <summary>ok | bad | empty</summary>
        public string StatusCss { get; set; } = "empty";
        public string StatusText { get; set; } = "Chưa trả lời";
    }

    // ===================== API Response Models =====================
    public class SubmitExamResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public SubmitExamData? Data { get; set; }
    }

    public class SubmitExamData
    {
        public string StudentCode { get; set; } = string.Empty;
        public int ShuffledExamPaperId { get; set; }
        public double? Score { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalQuestions { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string StudentAnswersString { get; set; } = string.Empty;
        public string AnswerKey { get; set; } = string.Empty;
    }

    // ===================== Component =====================
    public partial class Result : ComponentBase
    {
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private HttpClient Http { get; set; } = default!;

        // ---- State cho Razor ----
        protected ExamResultVm? result;
        protected bool isLoading = true;
        protected string errorMessage = string.Empty;

        protected List<HeatCell> HeatCells { get; set; } = new();

        // (Không bắt buộc UI dùng, nhưng để sẵn)
        protected int Rows { get; set; } = 7;
        protected int Cols { get; set; } = 1;

        protected double ScorePercentSafe =>
            result?.Data?.TotalQuestions > 0
                ? (double)result.Data.CorrectAnswers / result.Data.TotalQuestions * 100.0
                : 0.0;

        // ---- SVG điểm lớn (data URI) ----
        protected string ScoreSvgDataUri =>
            BuildScoreSvgDataUri(result?.Data?.Score ?? 0,
                                 result?.Data?.CorrectAnswers ?? 0,
                                 result?.Data?.TotalQuestions ?? 0);

        // ===================== Lifecycle =====================
        protected override async Task OnInitializedAsync()
        {
            await LoadExamResultAsync();
        }

        protected void Reload() => _ = LoadExamResultAsync();

        /// <summary>
        /// Đọc dữ liệu từ API submit exam
        /// </summary>
        private async Task LoadExamResultAsync()
        {
            try
            {
                isLoading = true;
                errorMessage = string.Empty;
                await Task.Delay(150);

                // Lấy studentExamSessionId từ localStorage
                var sessionIdStr = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "currentStudentExamSessionId");
                if (string.IsNullOrEmpty(sessionIdStr) || !int.TryParse(sessionIdStr, out int sessionId))
                {
                    errorMessage = "Không tìm thấy thông tin phiên thi.";
                    return;
                }

                // Gọi API để lấy kết quả exam
                var response = await GetExamResultFromApiAsync(sessionId);
                if (response != null)
                {
                    result = response;
                    HeatCells = BuildHeatCells(ParseAnswers(result.Data.StudentAnswersString, result.Data.AnswerKey), result.Data.TotalQuestions);
                    Cols = (int)Math.Ceiling(result.Data.TotalQuestions / (double)Rows);
                }
                else
                {
                    errorMessage = "Không thể lấy kết quả bài thi từ server.";
                    HeatCells = new();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Lỗi: {ex.Message}";
                HeatCells = new();
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Gọi API để lấy kết quả exam
        /// </summary>
        private async Task<ExamResultVm?> GetExamResultFromApiAsync(int studentExamSessionId)
        {
            try
            {
                // Gọi API submit exam để lấy kết quả
                var requestData = new { StudentExamSessionId = studentExamSessionId };
                var jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                
                var response = await Http.PostAsync("/api/Student/submit-exam", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var submitResponse = JsonSerializer.Deserialize<SubmitExamResponse>(responseContent);
                    
                    if (submitResponse?.Success == true && submitResponse.Data != null)
                    {
                        // Chuyển đổi từ SubmitExamData sang ExamResultVm
                        return new ExamResultVm
                        {
                            Data = new ExamResultDataVm
                            {
                                StudentCode = submitResponse.Data.StudentCode,
                                ShuffledExamPaperId = submitResponse.Data.ShuffledExamPaperId,
                                Score = submitResponse.Data.Score ?? 0,
                                CorrectAnswers = submitResponse.Data.CorrectAnswers ?? 0,
                                TotalQuestions = submitResponse.Data.TotalQuestions ?? 0,
                                StartTime = submitResponse.Data.StartTime,
                                EndTime = submitResponse.Data.EndTime,
                                StudentAnswersString = submitResponse.Data.StudentAnswersString,
                                AnswerKey = submitResponse.Data.AnswerKey
                            }
                        };
                    }
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        // ===================== Helpers =====================

        // Parse "(1:xx);(2:yy)" -> map; "-" => "" (chưa trả lời)
        private static Dictionary<int, string> ParseMap(string src)
        {
            var map = new Dictionary<int, string>();
            if (string.IsNullOrWhiteSpace(src)) return map;

            foreach (var part in src.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var m = Regex.Match(part, "\\((\\d+):([^)]+)\\)");
                if (!m.Success) continue;

                var q = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                var ans = m.Groups[2].Value.Trim();
                if (ans == "-") ans = ""; // chưa trả lời
                map[q] = ans;
            }
            return map;
        }

        private static List<AnswerDetail> ParseAnswers(string studentAnswersString, string answerKey)
        {
            var student = ParseMap(studentAnswersString);
            var key = ParseMap(answerKey);

            var res = new List<AnswerDetail>(capacity: Math.Max(student.Count, key.Count));
            foreach (var q in key.Keys.OrderBy(k => k))
            {
                student.TryGetValue(q, out var stu);
                res.Add(new AnswerDetail
                {
                    QuestionNo = q,
                    StudentAnswer = stu ?? "",
                    CorrectAnswer = key[q]
                });
            }
            return res;
        }

        private static List<HeatCell> BuildHeatCells(List<AnswerDetail> details, int totalQuestions)
        {
            var cells = new List<HeatCell>(capacity: totalQuestions);
            var map = details.ToDictionary(d => d.QuestionNo, d => d);

            for (int i = 1; i <= totalQuestions; i++)
            {
                if (!map.TryGetValue(i, out var d))
                {
                    cells.Add(new HeatCell { QuestionNo = i, StatusCss = "empty", StatusText = "Chưa trả lời" });
                    continue;
                }

                if (string.IsNullOrEmpty(d.StudentAnswer))
                {
                    cells.Add(new HeatCell { QuestionNo = i, StatusCss = "empty", StatusText = "Chưa trả lời" });
                }
                else if (d.StudentAnswer == d.CorrectAnswer)
                {
                    cells.Add(new HeatCell { QuestionNo = i, StatusCss = "ok", StatusText = "Đúng" });
                }
                else
                {
                    cells.Add(new HeatCell { QuestionNo = i, StatusCss = "bad", StatusText = "Sai" });
                }
            }

            return cells;
        }

        // ===================== SVG điểm (data URI) =====================
        private static string BuildScoreSvgDataUri(double score, int correct, int total,
                                                   int width = 560, int height = 220)
        {
            var s = score.ToString("0.00", CultureInfo.InvariantCulture);
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
        fill='#ffffff'>{s}</text>
  <text x='{width / 2}' y='{height - 18}' text-anchor='middle'
        font-family='Segoe UI,Roboto,Arial' font-size='18' fill='rgba(255,255,255,.9)'>
    {correct}/{total} câu đúng
  </text>
</svg>";

            var bytes = Encoding.UTF8.GetBytes(svg);
            return "data:image/svg+xml;base64," + Convert.ToBase64String(bytes);
        }
    }
}
