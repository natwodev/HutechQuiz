using System.Text.RegularExpressions;
using frontend_manage.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Linq;

namespace frontend_manage.Services;

/// <summary>
/// Service xử lý chung cho việc render đề thi cho sinh viên và preview
/// Bao gồm: normalize latex, xử lý audio/image paths, render HTML
/// </summary>
public interface IExamRenderingService
{
    /// <summary>
    /// Normalize và render HTML cho QuestionContent hoặc AnswerContent
    /// Xử lý: latex, audio, image, markers
    /// </summary>
    string NormalizeAndRenderContent(string? content, string? shuffledExamPaperCore = null, string? originalExamPaperCore = null);

    /// <summary>
    /// Lấy đường dẫn audio từ tên file
    /// Hỗ trợ cả EPZ format (Data/Audio/...) và ZIP format (Q6.mp3)
    /// </summary>
    string GetAudioUrl(string audioFileName, string? shuffledExamPaperCore = null, string? originalExamPaperCore = null);

    /// <summary>
    /// Lấy đường dẫn image từ tên file hoặc question ID
    /// Hỗ trợ cả EPZ format và ZIP format
    /// </summary>
    string GetImageUrl(string imageFileNameOrId, string? shuffledExamPaperCore = null, string? originalExamPaperCore = null);

    /// <summary>
    /// Xử lý audio path cho EPZ format (cũ): audio/... → Data/Audio/...
    /// </summary>
    string ProcessEPZAudioPath(string audioPath);

    /// <summary>
    /// Xử lý audio path cho ZIP format (mới): giữ nguyên tên file
    /// </summary>
    string ProcessZipAudioPath(string audioFileName);

    /// <summary>
    /// Xử lý flat questions list từ parent-child structure
    /// </summary>
    List<T> GetFlatQuestions<T>(IEnumerable<T> questions, Func<T, int?> getParentId, Func<T, IEnumerable<T>?> getChildren, Func<T, int> getOrder) where T : class;

    /// <summary>
    /// Tính số thứ tự câu hỏi (bỏ qua parent questions)
    /// </summary>
    int? GetQuestionNumber<T>(T question, int indexInFlatList, List<T> flatQuestions, Func<T, int?> getParentId, Func<T, IEnumerable<T>?> getChildren) where T : class;

    /// <summary>
    /// Đếm tổng số câu hỏi (chỉ đếm child questions của parent, không đếm parent)
    /// </summary>
    int GetTotalQuestionsCount<T>(IEnumerable<T> questions, Func<T, int?> getParentId, Func<T, IEnumerable<T>?> getChildren) where T : class;
}

public class ExamRenderingService : IExamRenderingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ExamRenderingService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public string NormalizeAndRenderContent(string? content, string? shuffledExamPaperCore = null, string? originalExamPaperCore = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        // 1. Bỏ các marker {<number>}
        var cleaned = Regex.Replace(
            content,
            @"\{<\d+>\}",
            string.Empty
        );

        // 1.2. Xử lý [stem]...[/stem] tags - loại bỏ thẻ và chỉ giữ lại nội dung bên trong
        // Format: "[stem] Nối cột A với cột B cho phù hợp: [/stem]"
        // Kết quả: "Nối cột A với cột B cho phù hợp:"
        cleaned = Regex.Replace(
            cleaned,
            @"\[stem\]([\s\S]*?)\[/stem\]",
            match => match.Groups[1].Value.Trim(),
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        // 1.5. Xử lý [matching] marker - loại bỏ phần A: và B: columns khi render QuestionContent
        // Vì MatchingQuestion component sẽ tự render columns từ Answers
        // Format: "...stem... [matching] \n\nA:\n1. ...\n2. ...\n\nB:\na. ...\nb. ..."
        // Chỉ giữ lại phần stem, loại bỏ [matching] và tất cả sau đó
        cleaned = Regex.Replace(
            cleaned,
            @"\s*\[matching\][\s\S]*",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        // 2. Xử lý [latex]...[/latex] tags
        // KaTeX auto-render cần delimiters như \(...\) hoặc $...$ để nhận diện công thức toán học
        // Nếu nội dung trong [latex]...[/latex] chưa có delimiters, tự động thêm \(...\)
        cleaned = Regex.Replace(
            cleaned,
            @"\[latex\]([\s\S]*?)\[/latex\]",
            match =>
            {
                var inner = match.Groups[1].Value.Trim();
                if (string.IsNullOrEmpty(inner))
                    return match.Value;

                // Kiểm tra xem nội dung đã có delimiters chưa
                // Delimiters: $$, $, \(, \[
                bool hasDelimiters = inner.StartsWith("$$") || 
                                     inner.StartsWith("$") || 
                                     inner.StartsWith("\\[") || 
                                     inner.StartsWith("\\(");

                string latexContent = inner;

                // Nếu chưa có delimiters, thêm \(...\) cho inline math
                if (!hasDelimiters)
                {
                    latexContent = $"\\({inner}\\)";
                }

                // Escape HTML để tránh XSS nhưng giữ nguyên các ký tự LaTeX
                var escaped = latexContent
                    .Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;");

                // Trả về với format [latex]...[/latex] để JavaScript katexInterop.js xử lý
                // katexInterop.js sẽ convert thành <span class="katex-custom">\(...\)</span>
                // và KaTeX auto-render sẽ render nội dung LaTeX
                return $"[latex]{escaped}[/latex]";
            },
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        // 3. Xử lý <audio>...</audio> → thêm controls + src trỏ về file mp3
        cleaned = Regex.Replace(
            cleaned,
            @"<audio>(.*?)</audio>",
            match =>
            {
                var inner = match.Groups[1].Value.Trim();
                if (string.IsNullOrEmpty(inner))
                    return match.Value;

                // Chuẩn hóa path
                var normalized = inner.Replace("\\", "/").TrimStart('/');

                // Tách logic: EPZ format vs ZIP format
                string audioPath;
                if (normalized.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
                {
                    // EPZ format: map audio/ENGx.mp3 → Data/Audio/ENGx.mp3
                    audioPath = ProcessEPZAudioPath(normalized);
                }
                else
                {
                    // ZIP format: giữ nguyên tên file (Q6.mp3)
                    audioPath = ProcessZipAudioPath(normalized);
                }

                var audioUrl = GetAudioUrl(audioPath, shuffledExamPaperCore, originalExamPaperCore);
                if (string.IsNullOrEmpty(audioUrl))
                    return match.Value;

                // Tạo unique audioId từ audioPath
                var audioId = $"audio-{audioPath.Replace("/", "-").Replace("\\", "-").Replace(".", "-")}";
                
                // Thêm data attributes để JavaScript có thể track play count
                // Note: questionId và studentExamSessionId sẽ được set bởi JavaScript sau khi render
                return $"<audio id=\"{audioId}\" controls src=\"{audioUrl}\" data-audio-path=\"{audioPath}\" class=\"exam-audio-player\"></audio>";
            },
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        // 3. Xử lý các format image khác nhau
        
        // Format 1: <img src="Images/Q5.jpg" ... /> hoặc <img src='Images/Q5.jpg' ... />
        cleaned = Regex.Replace(
            cleaned,
            @"<img\s+([^>]*\s+)?src=[""']Images/([^""']+)[""']([^>]*)>",
            match =>
            {
                var imageFileName = match.Groups[2].Value;
                var beforeSrc = match.Groups[1].Value ?? string.Empty;
                var afterSrc = match.Groups[3].Value ?? string.Empty;
                
                // Extract image ID (loại bỏ extension)
                var imageId = imageFileName.Split('.').FirstOrDefault() ?? imageFileName;
                var baseUrl = GetImageUrl(imageId, shuffledExamPaperCore, originalExamPaperCore);
                if (string.IsNullOrEmpty(baseUrl))
                    return match.Value;

                // Giữ nguyên các attributes khác, chỉ thay đổi src
                var attributes = beforeSrc + afterSrc;
                var newAttributes = Regex.Replace(attributes, @"\s*(alt|style|onerror)=[""'][^""']*[""']", string.Empty, RegexOptions.IgnoreCase);
                
                // Tạo img với fallback cho nhiều extension (.png, .jpg, .jpeg)
                var basePath = baseUrl.Replace($"/{imageId}", "");
                return $"<img{newAttributes} src=\"{basePath}/{imageId}.png\" onerror=\"this.onerror=null; this.src='{basePath}/{imageId}.jpg'; this.onerror=function(){{this.src='{basePath}/{imageId}.jpeg'; this.onerror=function(){{this.style.display='none';}};}}\" alt=\"Question {imageId}\" style=\"max-width: 100%; height: auto; display: block; margin: 1rem auto;\" />";
            },
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        // Format 2: <img src=Images/Q5.jpg ... /> (không có quotes)
        cleaned = Regex.Replace(
            cleaned,
            @"<img\s+([^>]*\s+)?src=Images/([^\s>]+)([^>]*)>",
            match =>
            {
                var imageFileName = match.Groups[2].Value;
                var beforeSrc = match.Groups[1].Value ?? string.Empty;
                var afterSrc = match.Groups[3].Value ?? string.Empty;
                
                var imageId = imageFileName.Split('.').FirstOrDefault() ?? imageFileName;
                var baseUrl = GetImageUrl(imageId, shuffledExamPaperCore, originalExamPaperCore);
                if (string.IsNullOrEmpty(baseUrl))
                    return match.Value;

                var attributes = beforeSrc + afterSrc;
                var newAttributes = Regex.Replace(attributes, @"\s*(alt|style|onerror)=[""']?[^""'\s>]*[""']?", string.Empty, RegexOptions.IgnoreCase);
                
                var basePath = baseUrl.Replace($"/{imageId}", "");
                return $"<img{newAttributes} src=\"{basePath}/{imageId}.png\" onerror=\"this.onerror=null; this.src='{basePath}/{imageId}.jpg'; this.onerror=function(){{this.src='{basePath}/{imageId}.jpeg'; this.onerror=function(){{this.style.display='none';}};}}\" alt=\"Question {imageId}\" style=\"max-width: 100%; height: auto; display: block; margin: 1rem auto;\" />";
            },
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        // Format 3: [image]Q1[/image] hoặc [image]Q5[/image]
        cleaned = Regex.Replace(
            cleaned,
            @"\[image\]([^\]]+)\[/image\]",
            match =>
            {
                var imageId = match.Groups[1].Value.Trim();
                var baseUrl = GetImageUrl(imageId, shuffledExamPaperCore, originalExamPaperCore);
                if (string.IsNullOrEmpty(baseUrl))
                    return match.Value;

                // Tạo img với fallback cho nhiều extension
                var basePath = baseUrl.Replace($"/{imageId}", "");
                return $"<img src=\"{basePath}/{imageId}.png\" onerror=\"this.onerror=null; this.src='{basePath}/{imageId}.jpg'; this.onerror=function(){{this.src='{basePath}/{imageId}.jpeg'; this.onerror=function(){{this.style.display='none';}};}}\" alt=\"Question {imageId}\" style=\"max-width: 100%; height: auto; display: block; margin: 1rem auto;\" />";
            },
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        return cleaned;
    }

    public string ProcessEPZAudioPath(string audioPath)
    {
        // EPZ format: map audio/ENGx.mp3 → Data/Audio/ENGx.mp3
        if (audioPath.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return "Data/Audio/" + audioPath.Substring("audio/".Length);
        }
        return audioPath;
    }

    public string ProcessZipAudioPath(string audioFileName)
    {
        // ZIP format: giữ nguyên tên file (Q6.mp3)
        return audioFileName;
    }

    public string GetAudioUrl(string audioFileName, string? shuffledExamPaperCore = null, string? originalExamPaperCore = null)
    {
        if (string.IsNullOrEmpty(audioFileName))
            return string.Empty;

        // Xác định folder name từ core
        string folderName;
        if (!string.IsNullOrEmpty(shuffledExamPaperCore))
        {
            folderName = shuffledExamPaperCore.Split('_')[0];
        }
        else if (!string.IsNullOrEmpty(originalExamPaperCore))
        {
            folderName = originalExamPaperCore.Split('_')[0];
        }
        else
        {
            return string.Empty;
        }

        // Lấy base address từ HttpClient
        var baseAddr = (_httpClient.BaseAddress?.ToString() ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrEmpty(baseAddr))
            return string.Empty;

        // Tách logic: EPZ format (cũ) vs ZIP format (mới)
        // EPZ format: audio có path Data/Audio/... → lấy phần sau Data/Audio/
        // ZIP format: audio là tên file trực tiếp (Q6.mp3)
        
        if (audioFileName.StartsWith("Data/Audio/", StringComparison.OrdinalIgnoreCase))
        {
            // EPZ format: Data/Audio/ENGx.mp3 → EPZ/{folderName}/ENGx.mp3
            var relativePath = audioFileName.Replace("Data/Audio/", "");
            return $"{baseAddr}/EPZ/{folderName}/{relativePath}";
        }

        // ZIP format mới: Q6.mp3 → EPZ/{folderName}/Q6.mp3
        return $"{baseAddr}/EPZ/{folderName}/{audioFileName}";
    }

    public string GetImageUrl(string imageFileNameOrId, string? shuffledExamPaperCore = null, string? originalExamPaperCore = null)
    {
        if (string.IsNullOrEmpty(imageFileNameOrId))
            return string.Empty;

        // Xác định folder name từ core
        string folderName;
        if (!string.IsNullOrEmpty(shuffledExamPaperCore))
        {
            folderName = shuffledExamPaperCore.Split('_')[0];
        }
        else if (!string.IsNullOrEmpty(originalExamPaperCore))
        {
            folderName = originalExamPaperCore.Split('_')[0];
        }
        else
        {
            return string.Empty;
        }

        // Lấy base address từ HttpClient
        var baseAddr = (_httpClient.BaseAddress?.ToString() ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrEmpty(baseAddr))
            return string.Empty;

        // Xử lý imageFileNameOrId
        var imageFileName = imageFileNameOrId.Trim();
        
        // Loại bỏ "Images/" prefix nếu có
        if (imageFileName.StartsWith("Images/", StringComparison.OrdinalIgnoreCase))
        {
            imageFileName = imageFileName.Substring("Images/".Length);
        }
        
        // Nếu không có extension, giữ nguyên để frontend có thể thử nhiều extension
        // Frontend sẽ thử .png, .jpg, .jpeg theo thứ tự
        if (!imageFileName.Contains('.'))
        {
            // Không thêm extension, để frontend tự xử lý với fallback
            imageFileName = imageFileNameOrId;
        }
        else
        {
            // Đảm bảo extension là lowercase để tránh case sensitivity
            var parts = imageFileName.Split('.');
            if (parts.Length > 1)
            {
                var ext = parts[parts.Length - 1].ToLower();
                var name = string.Join(".", parts.Take(parts.Length - 1));
                imageFileName = $"{name}.{ext}";
            }
        }

        return $"{baseAddr}/EPZ/{folderName}/Images/{imageFileName}";
    }

    public List<T> GetFlatQuestions<T>(
        IEnumerable<T> questions,
        Func<T, int?> getParentId,
        Func<T, IEnumerable<T>?> getChildren,
        Func<T, int> getOrder) where T : class
    {
        if (questions == null)
            return new List<T>();

        var flatQuestions = new List<T>();
        var parentQuestions = questions
            .Where(q => getParentId(q) == null)
            .OrderBy(getOrder)
            .ToList();

        foreach (var parentQ in parentQuestions)
        {
            var children = getChildren(parentQ);
            
            // Nếu là câu hỏi cha (có child questions)
            if (children != null && children.Any())
            {
                flatQuestions.Add(parentQ); // Thêm câu hỏi cha
                // Thêm các câu hỏi con
                foreach (var child in children.OrderBy(getOrder))
                {
                    flatQuestions.Add(child);
                }
            }
            // Nếu là câu hỏi độc lập (không có child)
            else
            {
                flatQuestions.Add(parentQ);
            }
        }

        return flatQuestions;
    }

    public int? GetQuestionNumber<T>(
        T question,
        int indexInFlatList,
        List<T> flatQuestions,
        Func<T, int?> getParentId,
        Func<T, IEnumerable<T>?> getChildren) where T : class
    {
        // Nếu là parent question (có child questions), không đánh số
        if (getParentId(question) == null)
        {
            var children = getChildren(question);
            if (children != null && children.Any())
            {
                return null; // Parent question không có số
            }
        }

        // Tính số thứ tự dựa trên các câu hỏi trước đó
        int number = 1;
        for (int i = 0; i < indexInFlatList; i++)
        {
            var q = flatQuestions[i];
            // Chỉ đếm các câu hỏi không phải parent question
            var isParent = getParentId(q) == null && getChildren(q) != null && getChildren(q)!.Any();
            if (!isParent)
            {
                number++;
            }
        }

        return number;
    }

    public int GetTotalQuestionsCount<T>(
        IEnumerable<T> questions,
        Func<T, int?> getParentId,
        Func<T, IEnumerable<T>?> getChildren) where T : class
    {
        if (questions == null)
            return 0;

        int count = 0;
        var parentQuestions = questions.Where(q => getParentId(q) == null);

        foreach (var q in parentQuestions)
        {
            var children = getChildren(q);
            var isParentQuestion = children != null && children.Any();

            if (isParentQuestion)
            {
                // Câu hỏi cha: chỉ đếm các câu hỏi con
                count += children!.Count();
            }
            else
            {
                // Câu hỏi độc lập: đếm chính nó
                count++;
            }
        }

        return count;
    }
}

