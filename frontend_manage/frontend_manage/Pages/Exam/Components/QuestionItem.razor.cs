using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using frontend_manage.DTOs;
using frontend_manage.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace frontend_manage.Pages.Exam.Components
{
    public partial class QuestionItem : ComponentBase
    {
        [Parameter] public QuestionStructureDto Question { get; set; } = new();
        [Parameter] public string? ShuffledExamPaperCore { get; set; }
        [Parameter] public EventCallback<(int questionId, object? value)> OnAnswered { get; set; }
        [Parameter] public string DisplayNumber { get; set; } = string.Empty;
        [Parameter] public int? SelectedAnswerId { get; set; }
        [Parameter] public Func<int, int?>? SelectedAnswerProvider { get; set; }
        [Parameter] public Func<int, string?>? LabelProvider { get; set; }

        [Inject] private IKaTeXService KaTeX { get; set; } = default!;

        // Dùng HttpClient để lấy BaseAddress backend (http://localhost:5163/)
        [Inject] private HttpClient HttpClient { get; set; } = default!;

        private ElementReference _root;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await KaTeX.RenderAsync(".katex-content");
        }

        protected override void OnParametersSet()
        {
            var resolved = SelectedAnswerId ?? SelectedAnswerProvider?.Invoke(Question.OriginalExamPaperDetailId);
            if (resolved != _selectedSingle)
            {
                _selectedSingle = resolved;
            }
        }

        protected bool IsMatching(QuestionStructureDto q)
        {
            return (q.QuestionContent ?? string.Empty).Contains("[matching]", StringComparison.OrdinalIgnoreCase);
        }

        protected string NormalizeLatex(string? content)
        {
            if (string.IsNullOrWhiteSpace(content)) return string.Empty;

            // 1. Bỏ các marker {<number>}
            var cleaned = Regex.Replace(
                content,
                @"\{<\d+>\}",
                string.Empty
            );

            // 2. Xử lý <audio>...</audio> → thêm controls + src trỏ về file mp3 trên backend
            cleaned = Regex.Replace(
                cleaned,
                @"<audio>(.*?)</audio>",
                match =>
                {
                    var inner = match.Groups[1].Value.Trim();
                    if (string.IsNullOrEmpty(inner))
                        return match.Value;

                    // Chuẩn hóa path (giữ cả thư mục con)
                    var normalized = inner.Replace("\\", "/").TrimStart('/');

                    // Nếu format là audio/ENGx.mp3 thì map về Data/Audio/ENGx.mp3
                    if (normalized.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
                    {
                        normalized = "Data/Audio/" + normalized.Substring("audio/".Length);
                    }

                    var relativePath = normalized;
                    if (string.IsNullOrEmpty(relativePath))
                        return match.Value;

                    var audioUrl = GetAudioPath(relativePath);
                    if (string.IsNullOrEmpty(audioUrl))
                        return match.Value;

                    return $"<audio controls src=\"{audioUrl}\"></audio>";
                },
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );

            return cleaned;
        }

        private string GetAudioPath(string audioFileName)
        {
            if (Question == null || string.IsNullOrEmpty(audioFileName))
                return string.Empty;

            // Lấy folder từ ShuffledExamPaperCore của đề đang thi (truyền từ trên xuống)
            var core = ShuffledExamPaperCore ?? string.Empty;
            if (string.IsNullOrEmpty(core))
                return string.Empty;

            var folderName = core.Split('_')[0];

            var baseAddr = (HttpClient.BaseAddress?.ToString() ?? "http://localhost:5163/").TrimEnd('/');

            return $"{baseAddr}/EPZ/{folderName}/{audioFileName}";
        }

        protected bool IsGroupParent(QuestionStructureDto q)
        {
            return q.ChildQuestions != null && q.ChildQuestions.Count > 0;
        }

        protected int? _selectedSingle;

        protected Task SelectSingle(int questionId, int answerId)
        {
            _selectedSingle = answerId;
            return OnAnswered.InvokeAsync((questionId, (object?)answerId));
        }

        protected Task SelectMatching(int questionId, int leftAnswerId, string rightAnswerId)
        {
            return OnAnswered.InvokeAsync((questionId, new { left = leftAnswerId, right = rightAnswerId }));
        }

        protected Task OnOptionKeyDown(KeyboardEventArgs e, int questionId, int answerId)
        {
            if (e.Key == "Enter" || e.Key == " ")
            {
                return SelectSingle(questionId, answerId);
            }
            return Task.CompletedTask;
        }
    }
}


