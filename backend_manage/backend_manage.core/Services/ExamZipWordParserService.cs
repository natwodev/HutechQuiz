using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace backend_manage.core.Services;

/// <summary>
/// Parser cho format đề thi mới với [question id="Q1"], [image], [audio], [latex], [answer], [exam permute=true]
/// </summary>
public sealed class ExamZipParsedParent
{
    public string ParentId { get; set; } = string.Empty; // P1, P2, ...
    public string Stem { get; set; } = string.Empty;
    public bool CanShuffle { get; set; } = true; // Từ permute attribute
}

public sealed class ExamZipParsedQuestion
{
    public string QuestionId { get; set; } = string.Empty; // Q1, Q2, ...
    public string? ParentId { get; set; } // P1, P2, ... nếu thuộc parent
    public string QuestionType { get; set; } = "mcq"; // mcq, short, fill, match
    public string Stem { get; set; } = string.Empty;
    public string? LatexContent { get; set; }
    public bool HasImage { get; set; }
    public bool HasAudio { get; set; }
    public char? CorrectAnswerLabel { get; set; } // A, B, C, D cho MCQ
    public string? CorrectAnswerText { get; set; } // Cho SHORT answer hoặc MATCH answer (A-1;B-2)
    public bool CanShuffle { get; set; } = true; // Từ exam permute attribute trong question tag
    public List<ExamZipParsedAnswer> Answers { get; set; } = new();
    
    // For matching questions
    public List<string> ColumnA { get; set; } = new(); // A. Scaffold, B. Row, ...
    public List<string> ColumnB { get; set; } = new(); // 1. Something, 2. Something, ...
}

public sealed class ExamZipParsedAnswer
{
    public char Label { get; set; } // A, B, C, D
    public string Content { get; set; } = string.Empty;
}

public static class ExamZipWordParserService
{
    /// <summary>
    /// Parse Word document với format mới: [question id="Q1"], [answer], [image], [audio], [latex], [parent]
    /// </summary>
    public static (List<ExamZipParsedParent> Parents, List<ExamZipParsedQuestion> Questions, bool PermuteEnabled) Parse(Stream stream)
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

        // Sử dụng phương pháp duyệt qua tất cả các phần tử con của body 
        // để hỗ trợ cả Paragraph và Table (nơi chứa câu hỏi/đáp án)
        foreach (var child in body.ChildElements)
        {
            if (child is Paragraph p)
            {
                ProcessParagraph(p, blocks);
            }
            else if (child is Table t)
            {
                foreach (var row in t.Elements<TableRow>())
                {
                    foreach (var cell in row.Elements<TableCell>())
                    {
                        foreach (var cellP in cell.Elements<Paragraph>())
                        {
                            ProcessParagraph(cellP, blocks);
                        }
                    }
                }
            }
        }

        return blocks;
    }

    private static void ProcessParagraph(Paragraph p, List<string> blocks)
    {
        var sb = new StringBuilder();
        foreach (var element in p.ChildElements)
        {
            ProcessElement(element, sb);
        }

        var line = sb.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(line))
        {
            blocks.Add(line);
        }
    }

    private static void ProcessElement(DocumentFormat.OpenXml.OpenXmlElement element, StringBuilder sb)
    {
        // Xử lý AlternateContent để tránh bị lặp nội dung (ví dụ: một cái là oMath, một cái là text thô)
        if (element.LocalName == "alternateContent")
        {
            // Thường Choice sẽ chứa nội dung "xịn" hơn (như oMath)
            var choice = element.ChildElements.FirstOrDefault(x => x.LocalName == "choice");
            if (choice != null)
            {
                foreach (var child in choice.ChildElements) ProcessElement(child, sb);
                return;
            }
            // Nếu không có Choice thì lấy Fallback
            var fallback = element.ChildElements.FirstOrDefault(x => x.LocalName == "fallback");
            if (fallback != null)
            {
                foreach (var child in fallback.ChildElements) ProcessElement(child, sb);
                return;
            }
        }

        if (element is Run run)
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
        // Sử dụng LocalName để nhận diện các thẻ Math (oMath và oMathPara) 
        // nhằm tránh lỗi thiếu assembly reference cho namespace DocumentFormat.OpenXml.Math
        else if (element.LocalName == "oMath")
        {
            var latex = ConvertOmmlToLatex(element);
            if (!string.IsNullOrWhiteSpace(latex))
            {
                sb.Append($" [latex]{latex}[/latex] ");
            }
        }
        else if (element.LocalName == "oMathPara")
        {
            foreach (var m in element.Descendants())
            {
                if (m.LocalName == "oMath")
                {
                    var latex = ConvertOmmlToLatex(m);
                    if (!string.IsNullOrWhiteSpace(latex))
                    {
                        sb.Append($" [latex]{latex}[/latex] ");
                    }
                }
            }
        }
        else if (element is Hyperlink hyperlink)
        {
            foreach (var child in hyperlink.ChildElements)
            {
                ProcessElement(child, sb);
            }
        }
        else if (element.HasChildren)
        {
            // Xử lý đệ quy cho các phần tử chứa khác (ví dụ: SmartTag, v.v.)
            foreach (var child in element.ChildElements)
            {
                ProcessElement(child, sb);
            }
        }
    }

    private static string ConvertOmmlToLatex(DocumentFormat.OpenXml.OpenXmlElement mathElement)
    {
        if (mathElement == null) return "";
        var sb = new StringBuilder();
        foreach (var child in mathElement.ChildElements)
        {
            sb.Append(TranslateOmmlElement(child));
        }
        return sb.ToString().Trim();
    }

    private static string TranslateOmmlElement(DocumentFormat.OpenXml.OpenXmlElement el)
    {
        switch (el.LocalName)
        {
            case "r": // Run
                var text = "";
                foreach (var child in el.ChildElements)
                {
                    if (child.LocalName == "t") text += child.InnerText;
                }
                return MapMathSymbols(text);

            case "f": // Fraction
                var num = el.ChildElements.FirstOrDefault(x => x.LocalName == "num");
                var den = el.ChildElements.FirstOrDefault(x => x.LocalName == "den");
                return $"\\frac{{{ConvertOmmlToLatex(num)}}}{{{ConvertOmmlToLatex(den)}}}";

            case "sSub": // Subscript
                var subBase = el.ChildElements.FirstOrDefault(x => x.LocalName == "e");
                var sub = el.ChildElements.FirstOrDefault(x => x.LocalName == "sub");
                return $"{ConvertOmmlToLatex(subBase)}_{{{ConvertOmmlToLatex(sub)}}}";

            case "sSup": // Superscript
                var supBase = el.ChildElements.FirstOrDefault(x => x.LocalName == "e");
                var sup = el.ChildElements.FirstOrDefault(x => x.LocalName == "sup");
                return $"{ConvertOmmlToLatex(supBase)}^{{{ConvertOmmlToLatex(sup)}}}";

            case "sSubSup": // Sub-Superscript
                var subSupBase = el.ChildElements.FirstOrDefault(x => x.LocalName == "e");
                var subVal = el.ChildElements.FirstOrDefault(x => x.LocalName == "sub");
                var supVal = el.ChildElements.FirstOrDefault(x => x.LocalName == "sup");
                return $"{ConvertOmmlToLatex(subSupBase)}_{{{ConvertOmmlToLatex(subVal)}}}^{{{ConvertOmmlToLatex(supVal)}}}";

            case "nary": // N-ary (Sum, Integral)
                var naryPr = el.ChildElements.FirstOrDefault(x => x.LocalName == "naryPr");
                var chrAttr = naryPr?.ChildElements.FirstOrDefault(x => x.LocalName == "chr")?.GetAttribute("val", "http://schemas.openxmlformats.org/officeDocument/2006/math");
                var chr = chrAttr?.Value ?? "∫";
                var op = MapMathSymbols(chr);
                
                var narySub = el.ChildElements.FirstOrDefault(x => x.LocalName == "sub");
                var narySup = el.ChildElements.FirstOrDefault(x => x.LocalName == "sup");
                var naryE = el.ChildElements.FirstOrDefault(x => x.LocalName == "e");
                
                var res = op;
                if (narySub != null) res += $"_{{{ConvertOmmlToLatex(narySub)}}}";
                if (narySup != null) res += $"^{{{ConvertOmmlToLatex(narySup)}}}";
                res += $" {ConvertOmmlToLatex(naryE)}";
                return res;

            case "d": // Delimiter (Parentheses)
                var dBase = el.ChildElements.FirstOrDefault(x => x.LocalName == "e");
                // Mặc định là ngoặc đơn, có thể lấy từ dPr nếu cần
                return $"\\left( {ConvertOmmlToLatex(dBase)} \\right)";

            case "limLow": // Limit
                var limBase = el.ChildElements.FirstOrDefault(x => x.LocalName == "e");
                var lim = el.ChildElements.FirstOrDefault(x => x.LocalName == "lim");
                return $"\\lim_{{{ConvertOmmlToLatex(lim)}}} {ConvertOmmlToLatex(limBase)}";

            case "rad": // Radical (Square root)
                var radBase = el.ChildElements.FirstOrDefault(x => x.LocalName == "e");
                var deg = el.ChildElements.FirstOrDefault(x => x.LocalName == "deg");
                if (deg != null && !string.IsNullOrEmpty(deg.InnerText))
                    return $"\\sqrt[{ConvertOmmlToLatex(deg)}]{{{ConvertOmmlToLatex(radBase)}}}";
                return $"\\sqrt{{{ConvertOmmlToLatex(radBase)}}}";

            case "m": // Matrix
                return "[Matrix]"; // Tạm thời chưa xử lý matrix phức tạp

            case "e": // Base/Element container
                var sbE = new StringBuilder();
                foreach (var child in el.ChildElements) sbE.Append(TranslateOmmlElement(child));
                return sbE.ToString();

            default:
                if (el.HasChildren)
                {
                    var sbDefault = new StringBuilder();
                    foreach (var child in el.ChildElements) sbDefault.Append(TranslateOmmlElement(child));
                    return sbDefault.ToString();
                }
                return MapMathSymbols(el.InnerText);
        }
    }

    private static string MapMathSymbols(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        var result = text;
        // Mapping các ký hiệu Unicode sang LaTeX command
        var maps = new Dictionary<string, string>
        {
            { "∬", "\\iint" },
            { "∭", "\\iiint" },
            { "∫", "\\int" },
            { "∑", "\\sum" },
            { "∏", "\\prod" },
            { "Ω", "\\Omega" },
            { "ω", "\\omega" },
            { "Δ", "\\Delta" },
            { "δ", "\\delta" },
            { "α", "\\alpha" },
            { "β", "\\beta" },
            { "γ", "\\gamma" },
            { "λ", "\\lambda" },
            { "π", "\\pi" },
            { "θ", "\\theta" },
            { "φ", "\\phi" },
            { "σ", "\\sigma" },
            { "μ", "\\mu" },
            { "τ", "\\tau" },
            { "ε", "\\epsilon" },
            { "ζ", "\\zeta" },
            { "η", "\\eta" },
            { "κ", "\\kappa" },
            { "ρ", "\\rho" },
            { "ψ", "\\psi" },
            { "χ", "\\chi" },
            { "→", "\\to" },
            { "∞", "\\infty" },
            { "√", "\\sqrt" },
            { "≤", "\\le" },
            { "≥", "\\ge" },
            { "≠", "\\neq" },
            { "≈", "\\approx" },
            { "±", "\\pm" },
            { "×", "\\times" },
            { "÷", "\\div" },
            { "⋅", "\\cdot" },
            { "∂", "\\partial" },
            { "∇", "\\nabla" },
            { "∀", "\\forall" },
            { "∃", "\\exists" },
            { "∈", "\\in" },
            { "∉", "\\notin" },
            { "⊂", "\\subset" },
            { "⊃", "\\supset" },
            { "∪", "\\cup" },
             { "∩", "\\cap" },
             { "∧", "\\wedge" },
             { "∨", "\\vee" },
             { "¬", "\\neg" },
             { "⇒", "\\Rightarrow" },
             { "⇔", "\\Leftrightarrow" },
             { "≡", "\\equiv" },
             { "≅", "\\cong" },
             { "∝", "\\propto" },
             { "∠", "\\angle" },
             { "⊥", "\\perp" },
             { "∥", "\\parallel" }
         };

        foreach (var map in maps)
        {
            // Thêm dấu cách sau mỗi command LaTeX để tránh bị dính vào ký tự sau (ví dụ: \DeltaV -> \Delta V)
            // Chỉ thêm nếu map.Value bắt đầu bằng \
            string replacement = map.Value;
            if (replacement.StartsWith("\\"))
            {
                replacement += " ";
            }
            result = result.Replace(map.Key, replacement);
        }

        return result;
    }

    // =========================
    // PARSE QUESTIONS
    // =========================
    private static (List<ExamZipParsedParent> Parents, List<ExamZipParsedQuestion> Questions, bool PermuteEnabled) ParseQuestions(List<string> lines)
    {
        var parents = new List<ExamZipParsedParent>();
        var questions = new List<ExamZipParsedQuestion>();
        // Mặc định cho phép hoán vị (giống logic EPZ) trừ khi user set false
        bool permuteEnabled = true;

        ExamZipParsedParent? currentParent = null;
        ExamZipParsedQuestion? current = null;
        ExamZipParsedAnswer? currentAnswer = null;

        // Regex patterns
        // Format: [question id="Q1", exam permute=true] hoặc [question id="Q1"]
        // Cần parse cả id, type, và exam permute attribute (có thể có dấu phẩy hoặc không)
        var examRegex = new Regex(@"\[exam\s+permute\s*=\s*['""]?(true|false)['""]?\]", RegexOptions.IgnoreCase);
        var parentRegex = new Regex(@"\[parent\s+id\s*=\s*""([^""]+)""(?:\s+permute\s*=\s*(true|false))?\s*\]", RegexOptions.IgnoreCase);
        var parentEndRegex = new Regex(@"\[/parent\]", RegexOptions.IgnoreCase);
        
        // Updated: Support both quoted and unquoted type values: type="match" or type=match or type=fill-multi
        // Groups: 1 = id, 2 = exam permute (trước type), 3 = type (quoted), 4 = type (unquoted), 5 = exam permute (sau type)
        var questionRegex = new Regex(@"\[question\s+id\s*=\s*""([^""]+)""(?:\s*,?\s*exam\s+permute\s*=\s*['""]?(true|false)['""]?)?(?:\s*,?\s*type\s*=\s*(?:""([^""]+)""|([a-zA-Z0-9\-]+)))?(?:\s*,?\s*exam\s+permute\s*=\s*['""]?(true|false)['""]?)?\s*\]", RegexOptions.IgnoreCase);
        var questionEndRegex = new Regex(@"\[/question\]", RegexOptions.IgnoreCase);
        var answerRegex = new Regex(@"\[answer\](.*?)\[/answer\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var answerStartRegex = new Regex(@"\[answer\]", RegexOptions.IgnoreCase);
        var answerEndRegex = new Regex(@"\[/answer\]", RegexOptions.IgnoreCase);
        var imageRegex = new Regex(@"\[image\]", RegexOptions.IgnoreCase);
        var audioRegex = new Regex(@"\[audio\]", RegexOptions.IgnoreCase);
        var latexRegex = new Regex(@"\[latex\](.*?)\[/latex\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var answerOptionRegex = new Regex(@"^([A-Z])[\.\\)]\s*(.*)$");
        var columnAStartRegex = new Regex(@"\[columnA\]", RegexOptions.IgnoreCase);
        var columnAEndRegex = new Regex(@"\[/columnA\]", RegexOptions.IgnoreCase);
        var columnBStartRegex = new Regex(@"\[columnB\]", RegexOptions.IgnoreCase);
        var columnBEndRegex = new Regex(@"\[/columnB\]", RegexOptions.IgnoreCase);
        
        // State variables for parsing multi-line blocks
        bool inAnswerBlock = false;
        bool inColumnABlock = false;
        bool inColumnBBlock = false;
        var answerBlockContent = new StringBuilder();

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            // ===== EXAM PERMUTE =====
            var examMatch = examRegex.Match(line);
            if (examMatch.Success)
            {
                if (examMatch.Groups.Count > 1)
                {
                    permuteEnabled = examMatch.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
                continue;
            }

            // ===== PARENT START =====
            var parentMatch = parentRegex.Match(line);
            if (parentMatch.Success)
            {
                // Flush previous parent nếu có
                if (currentParent != null)
                {
                    parents.Add(currentParent);
                }

                currentParent = new ExamZipParsedParent
                {
                    ParentId = parentMatch.Groups[1].Value.Trim(),
                    CanShuffle = parentMatch.Groups.Count > 2 && 
                                 parentMatch.Groups[2].Value.Equals("true", StringComparison.OrdinalIgnoreCase)
                };
                continue;
            }

            // ===== PARENT END =====
            if (parentEndRegex.IsMatch(line))
            {
                if (currentParent != null)
                {
                    parents.Add(currentParent);
                    currentParent = null;
                }
                continue;
            }

            // ===== QUESTION START =====
            var questionMatch = questionRegex.Match(line);
            if (questionMatch.Success)
            {
                // Flush previous question
                if (current != null)
                {
                    if (currentAnswer != null)
                        current.Answers.Add(currentAnswer);
                    
                    questions.Add(current);
                }

                // Parse question attributes
                // Groups: 1 = id, 2 = exam permute (nếu có dấu phẩy), 3 = type, 4 = exam permute (nếu không có dấu phẩy)
                var questionId = questionMatch.Groups[1].Value.Trim();
                
                // Lấy exam permute từ group 2 (có dấu phẩy) hoặc group 4 (không có dấu phẩy)
                var examPermuteValue = permuteEnabled; // Mặc định dùng giá trị global
                if (questionMatch.Groups.Count > 2 && !string.IsNullOrEmpty(questionMatch.Groups[2].Value))
                {
                    examPermuteValue = questionMatch.Groups[2].Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                }
                else if (questionMatch.Groups.Count > 5 && !string.IsNullOrEmpty(questionMatch.Groups[5].Value))
                {
                    examPermuteValue = questionMatch.Groups[5].Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                }
                
                // Lấy type từ group 3 (quoted) hoặc group 4 (unquoted)
                // Hỗ trợ: type="match" hoặc type=match hoặc type=fill-multi
                string questionType = "mcq";
                if (questionMatch.Groups.Count > 3 && !string.IsNullOrEmpty(questionMatch.Groups[3].Value))
                {
                    questionType = questionMatch.Groups[3].Value.Trim().ToLower();
                }
                else if (questionMatch.Groups.Count > 4 && !string.IsNullOrEmpty(questionMatch.Groups[4].Value))
                {
                    questionType = questionMatch.Groups[4].Value.Trim().ToLower();
                }
                
                // Normalize fill-multi to fill
                if (questionType.StartsWith("fill"))
                {
                    questionType = "fill";
                }

                // Hỗ trợ các loại câu hỏi: mcq, short, fill, match

                current = new ExamZipParsedQuestion
                {
                    QuestionId = questionId,
                    ParentId = currentParent?.ParentId, // Gán parentId nếu đang trong parent
                    QuestionType = questionType,
                    Stem = string.Empty,
                    Answers = new List<ExamZipParsedAnswer>(),
                    CanShuffle = examPermuteValue // Lưu giá trị permute từ question tag
                };
                
                // Cập nhật permuteEnabled nếu question có exam permute=true
                if (examPermuteValue)
                {
                    permuteEnabled = true;
                }
                
                currentAnswer = null;
                continue;
            }

            // ===== QUESTION END =====
            if (questionEndRegex.IsMatch(line))
            {
                if (current != null)
                {
                    if (currentAnswer != null)
                        current.Answers.Add(currentAnswer);
                    
                    questions.Add(current);
                    
                    current = null;
                    currentAnswer = null;
                }
                continue;
            }

            // ===== PARENT CONTENT =====
            if (currentParent != null && current == null)
            {
                // Đang trong parent nhưng chưa có question nào, đây là nội dung của parent
                currentParent.Stem += (string.IsNullOrEmpty(currentParent.Stem) ? "" : " ") + line;
                continue;
            }

            if (current == null) continue;

            // ===== ANSWER TAG (correct answer) =====
            var answerMatch = answerRegex.Match(line);
            if (answerMatch.Success)
            {
                var answerContent = answerMatch.Groups[1].Value.Trim();
                
                if (current.QuestionType == "short")
                {
                    current.CorrectAnswerText = answerContent;
                }
                else
                {
                    // MCQ: answer là một chữ cái A, B, C, D
                    if (answerContent.Length == 1 && char.IsLetter(answerContent[0]))
                    {
                        current.CorrectAnswerLabel = char.ToUpper(answerContent[0]);
                    }
                }
                continue;
            }

            // ===== IMAGE TAG =====
            if (imageRegex.IsMatch(line))
            {
                current.HasImage = true;
                // Image sẽ được lưu với tên = QuestionId trong folder Images
                // Không continue để tag [image] được thêm vào Stem/Content làm placeholder
            }

            // ===== AUDIO TAG =====
            if (audioRegex.IsMatch(line))
            {
                current.HasAudio = true;
                // Logic cũ: Nếu dòng chỉ chứa duy nhất tag audio, skip line
                if (line.Trim().Equals("[audio]", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                // Thêm trường hợp: Nếu audio nằm trong dòng văn bản, không skip để giữ nội dung
            }

            // ===== LATEX TAG =====
            var latexMatch = latexRegex.Match(line);
            if (latexMatch.Success)
            {
                // Logic cũ: Nếu dòng chỉ chứa duy nhất tag latex, gán vào LatexContent và skip
                if (line.Trim().Equals(latexMatch.Value.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    current.LatexContent = (current.LatexContent ?? "") + " " + latexMatch.Groups[1].Value.Trim();
                    continue;
                }
                
                // Thêm trường hợp: Nếu latex nằm trong dòng văn bản (inline) hoặc có nhiều tag,
                // ta không skip để nội dung (bao gồm cả các tag latex) được lưu vào Stem/Content.
                // Việc xử lý LatexContent ở đây được bỏ qua để tránh lặp nội dung khi ImportService append.
            }

            // ===== COLUMN A/B FOR MATCHING QUESTIONS (multi-line) =====
            if (current != null && current.QuestionType == "match")
            {
                // Handle [columnA] start
                if (columnAStartRegex.IsMatch(line))
                {
                    inColumnABlock = true;
                    // If content is on same line: [columnA]A. Something
                    var afterTag = columnAStartRegex.Replace(line, "").Trim();
                    if (!string.IsNullOrEmpty(afterTag) && !columnAEndRegex.IsMatch(afterTag))
                    {
                        current.ColumnA.Add(afterTag);
                    }
                    continue;
                }
                
                // Handle [/columnA] end
                if (columnAEndRegex.IsMatch(line))
                {
                    inColumnABlock = false;
                    continue;
                }
                
                // Collect columnA content
                if (inColumnABlock && !string.IsNullOrWhiteSpace(line))
                {
                    current.ColumnA.Add(line.Trim());
                    continue;
                }
                
                // Handle [columnB] start
                if (columnBStartRegex.IsMatch(line))
                {
                    inColumnBBlock = true;
                    var afterTag = columnBStartRegex.Replace(line, "").Trim();
                    if (!string.IsNullOrEmpty(afterTag) && !columnBEndRegex.IsMatch(afterTag))
                    {
                        current.ColumnB.Add(afterTag);
                    }
                    continue;
                }
                
                // Handle [/columnB] end
                if (columnBEndRegex.IsMatch(line))
                {
                    inColumnBBlock = false;
                    continue;
                }
                
                // Collect columnB content
                if (inColumnBBlock && !string.IsNullOrWhiteSpace(line))
                {
                    current.ColumnB.Add(line.Trim());
                    continue;
                }
            }
            
            // ===== MULTI-LINE ANSWER BLOCK =====
            if (current != null)
            {
                // Handle [answer] start (multi-line)
                if (answerStartRegex.IsMatch(line) && !answerEndRegex.IsMatch(line))
                {
                    inAnswerBlock = true;
                    answerBlockContent.Clear();
                    // Get content after [answer] tag on same line
                    var afterTag = answerStartRegex.Replace(line, "").Trim();
                    if (!string.IsNullOrEmpty(afterTag))
                    {
                        answerBlockContent.AppendLine(afterTag);
                    }
                    continue;
                }
                
                // Handle [/answer] end
                if (answerEndRegex.IsMatch(line))
                {
                    if (inAnswerBlock)
                    {
                        // Get content before [/answer] tag
                        var beforeTag = answerEndRegex.Replace(line, "").Trim();
                        if (!string.IsNullOrEmpty(beforeTag))
                        {
                            answerBlockContent.AppendLine(beforeTag);
                        }
                        
                        // Process the collected answer content
                        var answerContent = answerBlockContent.ToString().Trim();
                        if (current.QuestionType == "match")
                        {
                            // Match answer format: A-2 \n B-3 \n C-1 -> A-2;B-3;C-1
                            var pairs = answerContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Where(s => !string.IsNullOrEmpty(s));
                            current.CorrectAnswerText = string.Join(";", pairs);
                        }
                        else if (current.QuestionType == "fill")
                        {
                            // Fill answer format: StatelessWidget|StatefulWidget -> stored as CorrectAnswerText
                            // Không tạo answer options, chỉ lưu CorrectAnswerText để mobile hiển thị ô nhập
                            current.CorrectAnswerText = answerContent;
                        }
                        else if (current.QuestionType == "mcq" || current.QuestionType == "short")
                        {
                            // MCQ: single letter answer like "A" or "B"
                            if (answerContent.Length == 1 && char.IsLetter(answerContent[0]))
                            {
                                current.CorrectAnswerLabel = char.ToUpper(answerContent[0]);
                            }
                            else
                            {
                                current.CorrectAnswerText = answerContent;
                            }
                        }
                        
                        inAnswerBlock = false;
                    }
                    continue;
                }
                
                // Collect answer block content
                if (inAnswerBlock && !string.IsNullOrWhiteSpace(line))
                {
                    answerBlockContent.AppendLine(line);
                    continue;
                }
            }

            // ===== ANSWER OPTIONS (A. B. C. D.) for MCQ and FILL =====
            var ansOptionMatch = answerOptionRegex.Match(line);
            if (ansOptionMatch.Success && (current.QuestionType == "mcq" || current.QuestionType == "fill"))
            {
                if (currentAnswer != null)
                    current.Answers.Add(currentAnswer);

                currentAnswer = new ExamZipParsedAnswer
                {
                    Label = ansOptionMatch.Groups[1].Value[0],
                    Content = ansOptionMatch.Groups[2].Value.Trim()
                };
                continue;
            }

            // ===== MULTI-LINE CONTENT =====
            if (currentAnswer != null)
            {
                // Tiếp tục nội dung đáp án
                currentAnswer.Content += " " + line;
            }
            else
            {
                // Tiếp tục nội dung câu hỏi (stem)
                current.Stem += (string.IsNullOrEmpty(current.Stem) ? "" : " ") + line;
            }
        }

        // Flush last question
        if (current != null)
        {
            if (currentAnswer != null)
                current.Answers.Add(currentAnswer);
            
            questions.Add(current);
        }

        // Flush last parent
        if (currentParent != null)
        {
            parents.Add(currentParent);
        }

        return (parents, questions, permuteEnabled);
    }
}

