using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend_manage.core.Services;

public enum QuestionType
{
    Text,
    Latex,
    Image,
    Audio,
    Mixed
}

public sealed class ParsedQuestion
{
    public string CloCode { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public string Stem { get; set; } = string.Empty;
    public string? Latex { get; set; }
    public string? ImagePlaceholder { get; set; }
    public string? AudioPath { get; set; }
    public char? CorrectAnswerLabel { get; set; }
    public List<ParsedAnswer> Answers { get; set; } = new();
}

public sealed class ParsedAnswer
{
    public char Label { get; set; }
    public string Content { get; set; } = string.Empty;
}

public static class WordParserService
{
    public static List<ParsedQuestion> Parse(Stream stream)
    {
        var blocks = ReadBlocks(stream);
        return ParseQuestions(blocks);
    }

    // =========================
    // READ WORD BLOCKS
    // =========================
    private static List<string> ReadBlocks(Stream stream)
    {
        stream.Position = 0;
        using var mem = new MemoryStream();
        stream.CopyTo(mem);
        mem.Position = 0;

        using var doc = WordprocessingDocument.Open(mem, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        var blocks = new List<string>();

        if (body == null) return blocks;

        foreach (var p in body.Elements<Paragraph>())
        {
            var sb = new StringBuilder();

            foreach (var run in p.Elements<Run>())
            {
                if (run.Descendants<Drawing>().Any())
                {
                    sb.Append(" [[IMAGE]] ");
                }
                else
                {
                    sb.Append(run.InnerText);
                }
            }

            var line = sb.ToString().Trim();
            // Giữ lại marker ngắt câu hỏi theo format người dùng:
            // - Nếu paragraph trống => xem như một break (tương đương [<br>])
            // - Nếu paragraph có chữ => thêm nội dung như bình thường
            if (string.IsNullOrWhiteSpace(line))
            {
                blocks.Add("[[BR]]");
            }
            else
            {
                blocks.Add(line);
            }
        }

        return blocks;
    }

    // =========================
    // PARSE QUESTIONS
    // =========================
    private static List<ParsedQuestion> ParseQuestions(List<string> lines)
    {
        var result = new List<ParsedQuestion>();

        ParsedQuestion? current = null;
        ParsedAnswer? currentAnswer = null;

        var cloRegex = new Regex(@"^\(CLO(\d+)\)", RegexOptions.IgnoreCase);
        var answerRegex = new Regex(@"^([A-Z])[\.\)]\s*(.*)$");
        var audioRegex = new Regex(@"\[<audio>\](.*?)\[</audio>\]", RegexOptions.IgnoreCase);
        var answerKeyRegex = new Regex(@"^Đáp án[:：]\s*([A-Z])", RegexOptions.IgnoreCase);

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            // ===== BREAK (kết thúc 1 câu hỏi) =====
            // Hỗ trợ cả marker [<br>] trong nội dung và marker nội bộ [[BR]] (paragraph trống)
            if (IsBreakLine(line))
            {
                Flush(resetCurrent: true);
                continue;
            }

            // ===== CLO =====
            var cloMatch = cloRegex.Match(line);
            if (cloMatch.Success)
            {
                Flush(resetCurrent: false);

                current = new ParsedQuestion
                {
                    CloCode = "CLO" + cloMatch.Groups[1].Value,
                    Stem = line.Substring(cloMatch.Length).Trim(),
                    Type = QuestionType.Text
                };
                continue;
            }

            if (current == null) continue;

            // ===== AUDIO =====
            var audioMatch = audioRegex.Match(line);
            if (audioMatch.Success)
            {
                current.Type = QuestionType.Audio;
                current.AudioPath = audioMatch.Groups[1].Value.Trim();
                continue;
            }

            // ===== ANSWER KEY =====
            var keyMatch = answerKeyRegex.Match(line);
            if (keyMatch.Success)
            {
                current.CorrectAnswerLabel = keyMatch.Groups[1].Value[0];
                continue;
            }

            // ===== ANSWER =====
            var ansMatch = answerRegex.Match(line);
            if (ansMatch.Success)
            {
                if (currentAnswer != null)
                    current.Answers.Add(currentAnswer);

                currentAnswer = new ParsedAnswer
                {
                    Label = ansMatch.Groups[1].Value[0],
                    Content = ansMatch.Groups[2].Value.Trim()
                };
                continue;
            }

            // ===== IMAGE =====
            if (line.Contains("[[IMAGE]]"))
            {
                current.Type = current.Type == QuestionType.Text
                    ? QuestionType.Image
                    : QuestionType.Mixed;

                current.ImagePlaceholder = "[[IMAGE]]";
                continue;
            }

            // ===== LATEX =====
            if (line.Contains("\\") || line.Contains("∫"))
            {
                current.Type = current.Type == QuestionType.Text
                    ? QuestionType.Latex
                    : QuestionType.Mixed;

                current.Latex ??= "";
                current.Latex += " " + line;
                continue;
            }

            // ===== MULTI-LINE =====
            if (currentAnswer != null)
            {
                currentAnswer.Content += " " + line;
            }
            else
            {
                current.Stem += " " + line;
            }
        }

        Flush(resetCurrent: false);
        return result;

        void Flush(bool resetCurrent)
        {
            if (current == null) return;

            if (currentAnswer != null)
            {
                current.Answers.Add(currentAnswer);
                currentAnswer = null;
            }

            result.Add(current);
            if (resetCurrent)
            {
                current = null;
            }
        }
    }

    private static bool IsBreakLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return true;

        // paragraph trống đã được map sang [[BR]]
        if (line.Equals("[[BR]]", StringComparison.OrdinalIgnoreCase)) return true;

        // format user: [<br>]
        // Cho phép có khoảng trắng bên trong ngoặc []
        var normalized = line.Replace(" ", string.Empty);
        return normalized.Equals("[<br>]", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("<br>", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("[<br/>]", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("<br/>", StringComparison.OrdinalIgnoreCase);
    }
}

