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
    public string QuestionType { get; set; } = "mcq"; // mcq, short, match
    public string Stem { get; set; } = string.Empty;
    public string? LatexContent { get; set; }
    public bool HasImage { get; set; }
    public bool HasAudio { get; set; }
    public char? CorrectAnswerLabel { get; set; } // A, B, C, D cho MCQ
    public string? CorrectAnswerText { get; set; } // Cho SHORT answer
    public string? MatchAnswer { get; set; } // Cho MATCH type: "1-a;2-b;3-c"
    public bool CanShuffle { get; set; } = true; // Từ exam permute attribute trong question tag
    public List<ExamZipParsedAnswer> Answers { get; set; } = new();
    public List<string> MatchColumnA { get; set; } = new(); // Cho MATCH type
    public List<string> MatchColumnB { get; set; } = new(); // Cho MATCH type
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
            if (!string.IsNullOrWhiteSpace(line))
            {
                blocks.Add(line);
            }
        }

        return blocks;
    }

    // =========================
    // PARSE QUESTIONS
    // =========================
    private static (List<ExamZipParsedParent> Parents, List<ExamZipParsedQuestion> Questions, bool PermuteEnabled) ParseQuestions(List<string> lines)
    {
        var parents = new List<ExamZipParsedParent>();
        var questions = new List<ExamZipParsedQuestion>();
        bool permuteEnabled = false;

        ExamZipParsedParent? currentParent = null;
        ExamZipParsedQuestion? current = null;
        ExamZipParsedAnswer? currentAnswer = null;
        bool inMatchColumnA = false;
        bool inMatchColumnB = false;
        bool hasStemTag = false; // Đánh dấu đã có stem từ [stem]...[/stem]

        // Regex patterns
        // Format: [question id="Q1", exam permute=true] hoặc [question id="Q1"] hoặc [question id="Q9" type="match"]
        // Cần parse cả id, type, và exam permute attribute (có thể có dấu phẩy hoặc không)
        var examRegex = new Regex(@"\[exam\s+permute\s*=\s*true\]", RegexOptions.IgnoreCase);
        var parentRegex = new Regex(@"\[parent\s+id\s*=\s*""([^""]+)""(?:\s+permute\s*=\s*(true|false))?\s*\]", RegexOptions.IgnoreCase);
        var parentEndRegex = new Regex(@"\[/parent\]", RegexOptions.IgnoreCase);
        // Sửa regex để parse exam permute=true trong question tag
        // Format: [question id="Q1", exam permute=true] hoặc [question id="Q1" type="match"] hoặc [question id="Q1", exam permute=true type="match"]
        // Groups: 1 = id, 2 = exam permute (nếu có dấu phẩy trước), 3 = type, 4 = exam permute (nếu không có dấu phẩy, đứng sau type)
        // Regex pattern: id="..." (group 1), sau đó có thể có:
        //   - ", exam permute=true/false" (group 2) HOẶC
        //   - " type="..."" (group 3) HOẶC
        //   - cả hai theo thứ tự bất kỳ
        var questionRegex = new Regex(@"\[question\s+id\s*=\s*""([^""]+)""(?:\s*,\s*exam\s+permute\s*=\s*(true|false))?(?:\s+type\s*=\s*""([^""]+)"")?(?:\s+exam\s+permute\s*=\s*(true|false))?\s*\]", RegexOptions.IgnoreCase);
        var questionEndRegex = new Regex(@"\[/question\]", RegexOptions.IgnoreCase);
        var answerRegex = new Regex(@"\[answer\](.*?)\[/answer\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var imageRegex = new Regex(@"\[image\]", RegexOptions.IgnoreCase);
        var audioRegex = new Regex(@"\[audio\]", RegexOptions.IgnoreCase);
        var latexRegex = new Regex(@"\[latex\](.*?)\[/latex\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var stemRegex = new Regex(@"\[stem\](.*?)\[/stem\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var answerOptionRegex = new Regex(@"^([A-Z])[\.\)]\s*(.*)$");
        var matchColumnRegex = new Regex(@"^([AB]):\s*$", RegexOptions.IgnoreCase);

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            // ===== EXAM PERMUTE =====
            if (examRegex.IsMatch(line))
            {
                permuteEnabled = true;
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
                    
                    // Nếu là matching question, xử lý như parent-child structure
                    if (current.QuestionType == "match" && !string.IsNullOrEmpty(current.Stem) && 
                        current.MatchColumnA.Count > 0 && current.MatchColumnB.Count > 0)
                    {
                        // Tạo parent question từ stem
                        var parentQuestion = new ExamZipParsedParent
                        {
                            ParentId = current.QuestionId + "_parent", // Q9_parent
                            Stem = current.Stem.Trim(),
                            CanShuffle = currentParent?.CanShuffle ?? true
                        };
                        parents.Add(parentQuestion);
                        
                        // Tạo child questions từ cột A
                        for (int i = 0; i < current.MatchColumnA.Count; i++)
                        {
                            var childQuestion = new ExamZipParsedQuestion
                            {
                                QuestionId = current.QuestionId + "_child_" + (i + 1), // Q9_child_1, Q9_child_2, ...
                                ParentId = parentQuestion.ParentId,
                                QuestionType = "mcq", // Child questions là MCQ với answers từ cột B
                                Stem = current.MatchColumnA[i],
                                Answers = new List<ExamZipParsedAnswer>(),
                                MatchColumnA = new List<string>(),
                                MatchColumnB = new List<string>(),
                                CanShuffle = false // Child questions của matching question không được hoán vị
                            };
                            
                            // Thêm answers từ cột B
                            for (int j = 0; j < current.MatchColumnB.Count; j++)
                            {
                                var answerLabel = (char)('A' + j);
                                childQuestion.Answers.Add(new ExamZipParsedAnswer
                                {
                                    Label = answerLabel,
                                    Content = current.MatchColumnB[j]
                                });
                            }
                            
                            // Xác định CorrectAnswerLabel từ MatchAnswer (format: "1-a;2-b;3-c")
                            if (!string.IsNullOrEmpty(current.MatchAnswer))
                            {
                                var pairs = current.MatchAnswer.Split(';');
                                foreach (var pair in pairs)
                                {
                                    var parts = pair.Split('-');
                                    if (parts.Length == 2)
                                    {
                                        var leftIndexStr = parts[0].Trim();
                                        var rightLetter = parts[1].Trim().ToLower();
                                        
                                        if (int.TryParse(leftIndexStr, out int leftIndex) && 
                                            leftIndex - 1 == i && rightLetter.Length > 0)
                                        {
                                            // Tìm answer label tương ứng với rightLetter
                                            var rightIndex = rightLetter[0] - 'a';
                                            if (rightIndex >= 0 && rightIndex < childQuestion.Answers.Count)
                                            {
                                                childQuestion.CorrectAnswerLabel = childQuestion.Answers[rightIndex].Label;
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                            
                            questions.Add(childQuestion);
                        }
                    }
                    else
                    {
                        // Câu hỏi thường, thêm vào danh sách như bình thường
                        questions.Add(current);
                    }
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
                else if (questionMatch.Groups.Count > 4 && !string.IsNullOrEmpty(questionMatch.Groups[4].Value))
                {
                    examPermuteValue = questionMatch.Groups[4].Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                }
                
                // Lấy type từ group 3
                var questionType = questionMatch.Groups.Count > 3 && !string.IsNullOrEmpty(questionMatch.Groups[3].Value)
                    ? questionMatch.Groups[3].Value.Trim().ToLower()
                    : "mcq";

                current = new ExamZipParsedQuestion
                {
                    QuestionId = questionId,
                    ParentId = currentParent?.ParentId, // Gán parentId nếu đang trong parent
                    QuestionType = questionType,
                    Stem = string.Empty,
                    Answers = new List<ExamZipParsedAnswer>(),
                    MatchColumnA = new List<string>(),
                    MatchColumnB = new List<string>(),
                    CanShuffle = examPermuteValue // Lưu giá trị permute từ question tag
                };
                
                // Cập nhật permuteEnabled nếu question có exam permute=true
                if (examPermuteValue)
                {
                    permuteEnabled = true;
                }
                
                currentAnswer = null;
                inMatchColumnA = false;
                inMatchColumnB = false;
                hasStemTag = false; // Reset flag khi bắt đầu question mới
                continue;
            }

            // ===== QUESTION END =====
            if (questionEndRegex.IsMatch(line))
            {
                if (current != null)
                {
                    if (currentAnswer != null)
                        current.Answers.Add(currentAnswer);
                    
                    // Nếu là matching question, xử lý như parent-child structure
                    if (current.QuestionType == "match" && !string.IsNullOrEmpty(current.Stem) && 
                        current.MatchColumnA.Count > 0 && current.MatchColumnB.Count > 0)
                    {
                        // Tạo parent question từ stem
                        var parentQuestion = new ExamZipParsedParent
                        {
                            ParentId = current.QuestionId + "_parent", // Q9_parent
                            Stem = current.Stem.Trim(),
                            CanShuffle = currentParent?.CanShuffle ?? true
                        };
                        parents.Add(parentQuestion);
                        
                        // Tạo child questions từ cột A
                        for (int i = 0; i < current.MatchColumnA.Count; i++)
                        {
                            var childQuestion = new ExamZipParsedQuestion
                            {
                                QuestionId = current.QuestionId + "_child_" + (i + 1), // Q9_child_1, Q9_child_2, ...
                                ParentId = parentQuestion.ParentId,
                                QuestionType = "mcq", // Child questions là MCQ với answers từ cột B
                                Stem = current.MatchColumnA[i],
                                Answers = new List<ExamZipParsedAnswer>(),
                                MatchColumnA = new List<string>(),
                                MatchColumnB = new List<string>(),
                                CanShuffle = false // Child questions của matching question không được hoán vị
                            };
                            
                            // Thêm answers từ cột B
                            for (int j = 0; j < current.MatchColumnB.Count; j++)
                            {
                                var answerLabel = (char)('A' + j);
                                childQuestion.Answers.Add(new ExamZipParsedAnswer
                                {
                                    Label = answerLabel,
                                    Content = current.MatchColumnB[j]
                                });
                            }
                            
                            // Xác định CorrectAnswerLabel từ MatchAnswer (format: "1-a;2-b;3-c")
                            if (!string.IsNullOrEmpty(current.MatchAnswer))
                            {
                                var pairs = current.MatchAnswer.Split(';');
                                foreach (var pair in pairs)
                                {
                                    var parts = pair.Split('-');
                                    if (parts.Length == 2)
                                    {
                                        var leftIndexStr = parts[0].Trim();
                                        var rightLetter = parts[1].Trim().ToLower();
                                        
                                        if (int.TryParse(leftIndexStr, out int leftIndex) && 
                                            leftIndex - 1 == i && rightLetter.Length > 0)
                                        {
                                            // Tìm answer label tương ứng với rightLetter
                                            var rightIndex = rightLetter[0] - 'a';
                                            if (rightIndex >= 0 && rightIndex < childQuestion.Answers.Count)
                                            {
                                                childQuestion.CorrectAnswerLabel = childQuestion.Answers[rightIndex].Label;
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                            
                            questions.Add(childQuestion);
                        }
                    }
                    else
                    {
                        // Câu hỏi thường, thêm vào danh sách như bình thường
                        questions.Add(current);
                    }
                    
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
                
                if (current.QuestionType == "match")
                {
                    current.MatchAnswer = answerContent; // Format: "1-a;2-b;3-c"
                }
                else if (current.QuestionType == "short")
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
                // Image sẽ được lưu với tên = QuestionId (ví dụ Q6.jpg)
                continue;
            }

            // ===== AUDIO TAG =====
            if (audioRegex.IsMatch(line))
            {
                current.HasAudio = true;
                // Audio sẽ được lưu với tên = QuestionId (ví dụ Q6.mp3)
                continue;
            }

            // ===== LATEX TAG =====
            var latexMatch = latexRegex.Match(line);
            if (latexMatch.Success)
            {
                current.LatexContent = (current.LatexContent ?? "") + " " + latexMatch.Groups[1].Value.Trim();
                continue;
            }

            // ===== STEM TAG (cho matching question) =====
            var stemMatch = stemRegex.Match(line);
            if (stemMatch.Success && current != null)
            {
                // Nội dung trong [stem]...[/stem] sẽ được lưu vào Stem của question
                // Đây là stem chính thức cho matching question, không thêm nội dung khác vào
                var stemContent = stemMatch.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(stemContent))
                {
                    current.Stem = stemContent;
                    hasStemTag = true; // Đánh dấu đã có stem từ tag
                }
                continue;
            }

            // ===== MATCH COLUMN HEADERS =====
            var matchColMatch = matchColumnRegex.Match(line);
            if (matchColMatch.Success)
            {
                var col = matchColMatch.Groups[1].Value.ToUpper();
                if (col == "A")
                {
                    inMatchColumnA = true;
                    inMatchColumnB = false;
                    // Không thêm "A:" vào stem
                }
                else if (col == "B")
                {
                    inMatchColumnA = false;
                    inMatchColumnB = true;
                    // Không thêm "B:" vào stem
                }
                continue;
            }

            // ===== ANSWER OPTIONS (A. B. C. D.) =====
            var ansOptionMatch = answerOptionRegex.Match(line);
            if (ansOptionMatch.Success && current.QuestionType == "mcq")
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

            // ===== MATCH COLUMN ITEMS =====
            if (current.QuestionType == "match")
            {
                if (inMatchColumnA)
                {
                    // Format: "1. Iodine" → chỉ lấy "Iodine" (loại bỏ số đầu)
                    var matchItemRegex = new Regex(@"^\d+\.\s*(.+)$");
                    var itemMatch = matchItemRegex.Match(line);
                    if (itemMatch.Success)
                    {
                        // Loại bỏ số đầu, chỉ lấy nội dung sau dấu chấm
                        var content = itemMatch.Groups[1].Value.Trim();
                        current.MatchColumnA.Add(content);
                    }
                    else if (!string.IsNullOrWhiteSpace(line) && 
                             !line.StartsWith("A:", StringComparison.OrdinalIgnoreCase) && 
                             !line.StartsWith("B:", StringComparison.OrdinalIgnoreCase) &&
                             !matchColumnRegex.IsMatch(line))
                    {
                        // Nếu không match format "1. ...", thử loại bỏ số đầu nếu có
                        var cleanedLine = Regex.Replace(line, @"^\d+\.\s*", string.Empty).Trim();
                        if (!string.IsNullOrWhiteSpace(cleanedLine))
                        {
                            current.MatchColumnA.Add(cleanedLine);
                        }
                    }
                }
                else if (inMatchColumnB)
                {
                    // Format: "a. Starch indicator" → chỉ lấy "Starch indicator" (loại bỏ chữ cái đầu)
                    var matchItemRegex = new Regex(@"^[a-z]\.\s*(.+)$", RegexOptions.IgnoreCase);
                    var itemMatch = matchItemRegex.Match(line);
                    if (itemMatch.Success)
                    {
                        // Loại bỏ chữ cái đầu, chỉ lấy nội dung sau dấu chấm
                        var content = itemMatch.Groups[1].Value.Trim();
                        current.MatchColumnB.Add(content);
                    }
                    else if (!string.IsNullOrWhiteSpace(line) && 
                             !line.StartsWith("A:", StringComparison.OrdinalIgnoreCase) && 
                             !line.StartsWith("B:", StringComparison.OrdinalIgnoreCase) &&
                             !matchColumnRegex.IsMatch(line))
                    {
                        // Nếu không match format "a. ...", thử loại bỏ chữ cái đầu nếu có
                        var cleanedLine = Regex.Replace(line, @"^[a-z]\.\s*", string.Empty, RegexOptions.IgnoreCase).Trim();
                        if (!string.IsNullOrWhiteSpace(cleanedLine))
                        {
                            current.MatchColumnB.Add(cleanedLine);
                        }
                    }
                }
                else
                {
                    // Nếu chưa vào column nào (A: hoặc B:), đây là stem
                    // Ví dụ: "Nối cột A với cột B cho phù hợp:"
                    // Chỉ thêm vào stem nếu không phải là các tag khác đã được xử lý ở trên
                    // VÀ chưa có stem từ [stem]...[/stem]
                    if (!hasStemTag && 
                        !answerMatch.Success && 
                        !imageRegex.IsMatch(line) && 
                        !audioRegex.IsMatch(line) && 
                        !latexMatch.Success &&
                        !matchColumnRegex.IsMatch(line))
                    {
                        current.Stem += (string.IsNullOrEmpty(current.Stem) ? "" : " ") + line;
                    }
                }
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
                // Chỉ thêm nếu chưa có stem từ [stem]...[/stem] hoặc không phải matching question
                if (current.QuestionType != "match" || !hasStemTag)
                {
                    current.Stem += (string.IsNullOrEmpty(current.Stem) ? "" : " ") + line;
                }
            }
        }

        // Flush last question
        if (current != null)
        {
            if (currentAnswer != null)
                current.Answers.Add(currentAnswer);
            
            // Nếu là matching question, xử lý như parent-child structure
            if (current.QuestionType == "match" && !string.IsNullOrEmpty(current.Stem) && 
                current.MatchColumnA.Count > 0 && current.MatchColumnB.Count > 0)
            {
                // Tạo parent question từ stem
                var parentQuestion = new ExamZipParsedParent
                {
                    ParentId = current.QuestionId + "_parent", // Q9_parent
                    Stem = current.Stem.Trim(),
                    CanShuffle = currentParent?.CanShuffle ?? true
                };
                parents.Add(parentQuestion);
                
                // Tạo child questions từ cột A
                for (int i = 0; i < current.MatchColumnA.Count; i++)
                {
                    var childQuestion = new ExamZipParsedQuestion
                    {
                        QuestionId = current.QuestionId + "_child_" + (i + 1), // Q9_child_1, Q9_child_2, ...
                        ParentId = parentQuestion.ParentId,
                        QuestionType = "mcq", // Child questions là MCQ với answers từ cột B
                        Stem = current.MatchColumnA[i],
                        Answers = new List<ExamZipParsedAnswer>(),
                        MatchColumnA = new List<string>(),
                        MatchColumnB = new List<string>()
                    };
                    
                    // Thêm answers từ cột B
                    for (int j = 0; j < current.MatchColumnB.Count; j++)
                    {
                        var answerLabel = (char)('A' + j);
                        childQuestion.Answers.Add(new ExamZipParsedAnswer
                        {
                            Label = answerLabel,
                            Content = current.MatchColumnB[j]
                        });
                    }
                    
                    // Xác định CorrectAnswerLabel từ MatchAnswer (format: "1-a;2-b;3-c")
                    if (!string.IsNullOrEmpty(current.MatchAnswer))
                    {
                        var pairs = current.MatchAnswer.Split(';');
                        foreach (var pair in pairs)
                        {
                            var parts = pair.Split('-');
                            if (parts.Length == 2)
                            {
                                var leftIndexStr = parts[0].Trim();
                                var rightLetter = parts[1].Trim().ToLower();
                                
                                if (int.TryParse(leftIndexStr, out int leftIndex) && 
                                    leftIndex - 1 == i && rightLetter.Length > 0)
                                {
                                    // Tìm answer label tương ứng với rightLetter
                                    var rightIndex = rightLetter[0] - 'a';
                                    if (rightIndex >= 0 && rightIndex < childQuestion.Answers.Count)
                                    {
                                        childQuestion.CorrectAnswerLabel = childQuestion.Answers[rightIndex].Label;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    
                    questions.Add(childQuestion);
                }
            }
            else
            {
                // Câu hỏi thường, thêm vào danh sách như bình thường
                questions.Add(current);
            }
        }

        // Flush last parent
        if (currentParent != null)
        {
            parents.Add(currentParent);
        }

        return (parents, questions, permuteEnabled);
    }
}

