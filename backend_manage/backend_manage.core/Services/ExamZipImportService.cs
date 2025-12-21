using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace backend_manage.core.Services;

/// <summary>
/// Service xử lý import đề thi từ ZIP file theo format mới
/// ZIP chứa: exam.docx, Audio/*.mp3, và images sẽ được extract từ Word
/// </summary>
public class ExamZipImportService
{
    private readonly IRepository<OriginalExamPaper> _originalExamPaperRepository;
    private readonly IRepository<OriginalExamPaperDetail> _originalExamPaperDetailRepository;
    private readonly IRepository<Answers> _answersRepository;
    private readonly IRepository<Subject> _subjectRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<ExamZipImportService> _logger;

    public ExamZipImportService(
        IRepository<OriginalExamPaper> originalExamPaperRepository,
        IRepository<OriginalExamPaperDetail> originalExamPaperDetailRepository,
        IRepository<Answers> answersRepository,
        IRepository<Subject> subjectRepository,
        IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment webHostEnvironment,
        ILogger<ExamZipImportService> logger)
    {
        _originalExamPaperRepository = originalExamPaperRepository;
        _originalExamPaperDetailRepository = originalExamPaperDetailRepository;
        _answersRepository = answersRepository;
        _subjectRepository = subjectRepository;
        _httpContextAccessor = httpContextAccessor;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    /// <summary>
    /// Import đề thi từ ZIP file
    /// </summary>
    public async Task ImportFromZipAsync(IFormFile zipFile, string originalExamPaperCore, int subjectId)
    {
        if (zipFile == null || zipFile.Length == 0)
            throw new ArgumentException("File ZIP không hợp lệ hoặc rỗng");

        if (!zipFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("File phải có đuôi .zip");

        // Kiểm tra mã đề thi đã tồn tại chưa
        var exists = await _originalExamPaperRepository.GetQueryable()
            .AnyAsync(x => x.OriginalExamPaperCore == originalExamPaperCore);
        if (exists)
            throw new Exception($"Đã tồn tại đề thi với mã '{originalExamPaperCore}' trong hệ thống.");

        // Lấy thông tin user
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("Không thể xác định người dùng tạo đề thi gốc.");

        // Kiểm tra môn học
        var subject = await _subjectRepository.GetQueryable()
            .FirstOrDefaultAsync(s => s.SubjectId == subjectId);
        if (subject == null)
            throw new Exception($"Không tìm thấy môn học với ID '{subjectId}'.");

        var now = DateTimeHelper.GetVietnamTime();

        // Tạo thư mục tạm để extract ZIP
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Extract ZIP file
            using (var zipStream = zipFile.OpenReadStream())
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                // Xóa thư mục nếu đã tồn tại để tránh conflict
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                Directory.CreateDirectory(tempDir);
                
                archive.ExtractToDirectory(tempDir);
            }

            // Tìm file .docx trong ZIP (tìm tất cả file .docx, không chỉ exam.docx)
            var docxFiles = Directory.GetFiles(tempDir, "*.docx", SearchOption.AllDirectories);
            
            if (docxFiles.Length == 0)
            {
                // Liệt kê tất cả các file trong thư mục để debug
                var allFiles = Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories);
                var fileList = string.Join(", ", allFiles.Select(f => Path.GetFileName(f)));
                throw new Exception($"Không tìm thấy file Word (.docx) trong ZIP file. Các file có trong ZIP: {fileList}");
            }

            // Ưu tiên file exam.docx, nếu không có thì lấy file .docx đầu tiên
            var examDocxPath = docxFiles.FirstOrDefault(f => 
                Path.GetFileName(f).Equals("exam.docx", StringComparison.OrdinalIgnoreCase));
            
            if (examDocxPath == null)
            {
                examDocxPath = docxFiles[0];
                _logger.LogInformation("Không tìm thấy exam.docx, sử dụng file .docx đầu tiên: {FileName}", Path.GetFileName(examDocxPath));
            }

            // Parse Word document
            using var docxStream = File.OpenRead(examDocxPath);
            var (parsedParents, parsedQuestions, permuteEnabled) = ExamZipWordParserService.Parse(docxStream);

            if (parsedQuestions.Count == 0 && parsedParents.Count == 0)
                throw new Exception("Không tìm thấy câu hỏi nào trong file Word.");

            // Tạo thư mục lưu trữ cho đề thi này
            // Frontend lấy audio từ /EPZ/{folderName}/{audioFileName}
            // folderName = originalExamPaperCore.Split('_')[0]
            var folderName = originalExamPaperCore.Split('_')[0];
            var examStorageDir = Path.Combine(_webHostEnvironment.WebRootPath, "EPZ", folderName);
            Directory.CreateDirectory(examStorageDir);
            var audioDir = examStorageDir; // Audio lưu trực tiếp trong EPZ/{folderName}/
            var imageDir = Path.Combine(examStorageDir, "Images");
            Directory.CreateDirectory(imageDir);

            // Copy audio files từ ZIP vào thư mục Audio
            var audioSourceDir = Path.Combine(tempDir, "Audio");
            if (Directory.Exists(audioSourceDir))
            {
                var audioFiles = Directory.GetFiles(audioSourceDir, "*.*", SearchOption.TopDirectoryOnly);
                foreach (var audioFile in audioFiles)
                {
                    var fileName = Path.GetFileName(audioFile);
                    var destPath = Path.Combine(audioDir, fileName);
                    File.Copy(audioFile, destPath, overwrite: true);
                    _logger.LogInformation("Copied audio file: {FileName} to {DestPath}", fileName, destPath);
                }
            }

            // Extract images từ Word document và lưu vào thư mục Images
            // Lấy tất cả questions (chỉ child questions có thể có image, parent chỉ có text)
            await ExtractImagesFromWordAsync(examDocxPath, imageDir, parsedQuestions);

            // Tạo OriginalExamPaper
            var totalQuestions = parsedParents.Count + parsedQuestions.Count; // Bao gồm cả parent và child
            var originalExamPaper = new OriginalExamPaper
            {
                Title = Path.GetFileNameWithoutExtension(zipFile.FileName),
                Description = null,
                SubjectId = subject.SubjectId,
                CreatedBy = userId,
                CreatedAt = now,
                DurationMinutes = 0,
                TotalQuestions = totalQuestions,
                OriginalExamPaperCore = originalExamPaperCore,
                IsApproved = true,
                IsManualCreated = false,
                TotalShuffledPapers = 0,
                AllowViewMaterials = false
            };

            await _originalExamPaperRepository.AddAsync(originalExamPaper);

            // Dictionary để map ParentId -> OriginalExamPaperDetailId (sau khi lưu vào DB)
            var parentIdMap = new Dictionary<string, int>();
            var answers = new List<Answers>();

            int globalOrder = 1;

            // Bước 1: Lưu parent questions vào database TRƯỚC để có ID
            // Xác định các parent questions là matching questions (có child questions với type="match")
            var matchingParentIds = new HashSet<string>();
            foreach (var parentGroup in parsedQuestions.Where(q => !string.IsNullOrEmpty(q.ParentId)).GroupBy(q => q.ParentId))
            {
                // Nếu tất cả child questions trong group này đều là match type, thì parent là matching question
                if (parentGroup.All(q => q.QuestionType == "match"))
                {
                    matchingParentIds.Add(parentGroup.Key);
                }
            }

            foreach (var parent in parsedParents)
            {
                // Nếu là matching question, không cho phép hoán vị parent question
                bool isMatchingParent = matchingParentIds.Contains(parent.ParentId);
                
                // Sử dụng giá trị CanShuffle từ parent (từ [parent permute=true] hoặc global permuteEnabled)
                // Trừ khi là matching question (matching questions không được hoán vị)
                // Giống EPZ import: CanShuffleQuestion = parentCauHoi.HoanVi
                bool canShuffleParent = isMatchingParent ? false : parent.CanShuffle;
                
                var parentDetail = new OriginalExamPaperDetail
                {
                    OriginalExamPaperId = originalExamPaper.OriginalExamPaperId,
                    Order = globalOrder++,
                    QuestionContent = parent.Stem,
                    CorrectAnswerIndex = null, // Parent không có đáp án đúng
                    ParentQuestionId = null, // Parent không có parent
                    CanShuffleQuestion = canShuffleParent,
                    ChapterId = null,
                    CreatedAt = now,
                    CreatedBy = userId
                };

                // Lưu vào database ngay để có ID
                await _originalExamPaperDetailRepository.AddAsync(parentDetail);
                parentIdMap[parent.ParentId] = parentDetail.OriginalExamPaperDetailId;
            }

            // Bước 2: Nhóm child questions theo parent
            var childQuestionsByParent = parsedQuestions
                .Where(q => !string.IsNullOrEmpty(q.ParentId))
                .GroupBy(q => q.ParentId)
                .ToList();

            var independentQuestions = parsedQuestions
                .Where(q => string.IsNullOrEmpty(q.ParentId))
                .ToList();

            // Bước 3: Lưu child questions theo từng nhóm parent
            foreach (var parentGroup in childQuestionsByParent)
            {
                var parentId = parentGroup.Key;
                if (!parentIdMap.ContainsKey(parentId))
                {
                    _logger.LogWarning("Không tìm thấy parent với ID: {ParentId}, bỏ qua các child questions", parentId);
                    continue;
                }

                var parentQuestionId = parentIdMap[parentId];
                int childOrder = 1; // Order riêng cho từng nhóm child
                
                // Kiểm tra xem parent này có phải là matching question không
                bool isMatchingParent = matchingParentIds.Contains(parentId);

                foreach (var q in parentGroup)
                {
                    // Xây dựng QuestionContent với các markers
                    var questionContent = BuildQuestionContent(q, originalExamPaperCore);

                    // Xác định CanShuffleQuestion:
                    // - Nếu là matching parent: không được hoán vị
                    // - Nếu không phải matching: dùng giá trị từ question tag (q.CanShuffle) hoặc permuteEnabled global
                    bool canShuffle = isMatchingParent ? false : (q.CanShuffle || permuteEnabled);

                    var detail = new OriginalExamPaperDetail
                    {
                        OriginalExamPaperId = originalExamPaper.OriginalExamPaperId,
                        Order = childOrder++, // Order riêng cho từng nhóm child (bắt đầu từ 1)
                        QuestionContent = questionContent,
                        CorrectAnswerIndex = q.QuestionType == "mcq" && q.CorrectAnswerLabel != null
                            ? q.Answers.FindIndex(a => a.Label == q.CorrectAnswerLabel) + 1
                            : null,
                        ParentQuestionId = parentQuestionId, // Liên kết với parent đã lưu
                        CanShuffleQuestion = canShuffle, // Sử dụng giá trị từ question tag hoặc global
                        ChapterId = null,
                        CreatedAt = now,
                        CreatedBy = userId
                    };

                    // Lưu vào database ngay
                    await _originalExamPaperDetailRepository.AddAsync(detail);

                    // Xử lý đáp án cho child question
                    // Với matching questions (parent-child structure), answers không được hoán vị
                    // Sử dụng giá trị từ question tag (q.CanShuffle) hoặc global permuteEnabled, giống EPZ import
                    bool canShuffleAnswers = isMatchingParent ? false : (q.CanShuffle || permuteEnabled);
                    
                    if (q.QuestionType == "mcq")
                    {
                        int aOrder = 1;
                        foreach (var a in q.Answers)
                        {
                            answers.Add(new Answers
                            {
                                OriginalExamPaperDetail = detail,
                                Order = aOrder++,
                                AnswerContent = a.Content,
                                IsCorrect = q.CorrectAnswerLabel == a.Label,
                                CanShuffleAnswer = canShuffleAnswers, // Không hoán vị nếu là matching parent
                                CreatedAt = now,
                                CreatedBy = userId
                            });
                        }
                    }
                    else if (q.QuestionType == "short")
                    {
                        // Câu hỏi tự luận: lưu đáp án đúng vào AnswerContent của một Answer
                        answers.Add(new Answers
                        {
                            OriginalExamPaperDetail = detail,
                            Order = 1,
                            AnswerContent = q.CorrectAnswerText ?? "",
                            IsCorrect = true,
                            CanShuffleAnswer = false, // Short answer không được hoán vị
                            CreatedAt = now,
                            CreatedBy = userId
                        });
                    }
                    else if (q.QuestionType == "match")
                    {
                        // Câu hỏi nối cột: tạo Answers từ MatchColumnA và MatchColumnB
                        // MatchColumnA → LeftItems (nửa đầu), MatchColumnB → RightItems (nửa sau)
                        // Matching questions không được hoán vị answers
                        int order = 1;
                        
                        // Thêm các items từ cột A (left) - nửa đầu của Answers
                        foreach (var itemA in q.MatchColumnA)
                        {
                            answers.Add(new Answers
                            {
                                OriginalExamPaperDetail = detail,
                                Order = order++,
                                AnswerContent = itemA,
                                IsCorrect = false, // Sẽ được validate khi match
                                CanShuffleAnswer = false, // Matching questions không được hoán vị answers
                                CreatedAt = now,
                                CreatedBy = userId
                            });
                        }
                        
                        // Thêm các items từ cột B (right) - nửa sau của Answers
                        foreach (var itemB in q.MatchColumnB)
                        {
                            answers.Add(new Answers
                            {
                                OriginalExamPaperDetail = detail,
                                Order = order++,
                                AnswerContent = itemB,
                                IsCorrect = false,
                                CanShuffleAnswer = false, // Matching questions không được hoán vị answers
                                CreatedAt = now,
                                CreatedBy = userId
                            });
                        }
                    }
                }
            }

            // Bước 4: Lưu các câu hỏi độc lập (không có parent)
            foreach (var q in independentQuestions)
            {
                // Xây dựng QuestionContent với các markers
                var questionContent = BuildQuestionContent(q, originalExamPaperCore);

                // Sử dụng giá trị CanShuffle từ question tag, nếu không có thì dùng permuteEnabled global
                bool canShuffle = q.CanShuffle || permuteEnabled;

                var detail = new OriginalExamPaperDetail
                {
                    OriginalExamPaperId = originalExamPaper.OriginalExamPaperId,
                    Order = globalOrder++,
                    QuestionContent = questionContent,
                    CorrectAnswerIndex = q.QuestionType == "mcq" && q.CorrectAnswerLabel != null
                        ? q.Answers.FindIndex(a => a.Label == q.CorrectAnswerLabel) + 1
                        : null,
                    ParentQuestionId = null, // Câu hỏi độc lập
                    CanShuffleQuestion = canShuffle, // Sử dụng giá trị từ question tag hoặc global
                    ChapterId = null,
                    CreatedAt = now,
                    CreatedBy = userId
                };

                // Lưu vào database
                await _originalExamPaperDetailRepository.AddAsync(detail);

                // Xử lý đáp án cho câu hỏi độc lập
                // Sử dụng giá trị CanShuffle từ question tag hoặc global permuteEnabled
                bool canShuffleAnswers = q.CanShuffle || permuteEnabled;
                
                if (q.QuestionType == "mcq")
                {
                    int aOrder = 1;
                    foreach (var a in q.Answers)
                    {
                        answers.Add(new Answers
                        {
                            OriginalExamPaperDetail = detail,
                            Order = aOrder++,
                            AnswerContent = a.Content,
                            IsCorrect = q.CorrectAnswerLabel == a.Label,
                            CanShuffleAnswer = canShuffleAnswers, // Sử dụng giá trị từ question tag hoặc global
                            CreatedAt = now,
                            CreatedBy = userId
                        });
                    }
                }
                else if (q.QuestionType == "short")
                {
                    answers.Add(new Answers
                    {
                        OriginalExamPaperDetail = detail,
                        Order = 1,
                        AnswerContent = q.CorrectAnswerText ?? "",
                        IsCorrect = true,
                        CanShuffleAnswer = false, // Short answer không được hoán vị
                        CreatedAt = now,
                        CreatedBy = userId
                    });
                }
                else if (q.QuestionType == "match")
                {
                    // Câu hỏi nối cột: tạo Answers từ MatchColumnA và MatchColumnB
                    // Matching questions độc lập không được hoán vị answers
                    int order = 1;
                    
                    // Thêm các items từ cột A (left) - nửa đầu của Answers
                    foreach (var itemA in q.MatchColumnA)
                    {
                        answers.Add(new Answers
                        {
                            OriginalExamPaperDetail = detail,
                            Order = order++,
                            AnswerContent = itemA,
                            IsCorrect = false,
                            CanShuffleAnswer = false, // Matching questions không được hoán vị answers
                            CreatedAt = now,
                            CreatedBy = userId
                        });
                    }
                    
                    // Thêm các items từ cột B (right) - nửa sau của Answers
                    foreach (var itemB in q.MatchColumnB)
                    {
                        answers.Add(new Answers
                        {
                            OriginalExamPaperDetail = detail,
                            Order = order++,
                            AnswerContent = itemB,
                            IsCorrect = false,
                            CanShuffleAnswer = false, // Matching questions không được hoán vị answers
                            CreatedAt = now,
                            CreatedBy = userId
                        });
                    }
                }
            }

            // Bước 5: Lưu tất cả answers vào database
            foreach (var answer in answers)
            {
                await _answersRepository.AddAsync(answer);
            }

            // Bước 6: Tạo KeyValueList giống EPZ import format: (QuestionId:AnswerId);(QuestionId:AnswerId);...
            // KeyValueList lưu mapping giữa OriginalExamPaperDetailId và AnswerId của đáp án đúng
            await UpdateKeyValueListAsync(originalExamPaper.OriginalExamPaperId);

            _logger.LogInformation("Import đề thi từ ZIP thành công: {OriginalExamPaperCore}, {TotalQuestions} câu hỏi", 
                originalExamPaperCore, parsedQuestions.Count);
        }
        finally
        {
            // Xóa thư mục tạm
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể xóa thư mục tạm: {TempDir}", tempDir);
                }
            }
        }
    }

    /// <summary>
    /// Xây dựng QuestionContent với các markers cho image, audio, latex
    /// Format phải tương thích với frontend: <audio>...</audio> cho audio, giữ nguyên [latex]...[/latex]
    /// </summary>
    private string BuildQuestionContent(ExamZipParsedQuestion q, string originalExamPaperCore)
    {
        var content = new StringBuilder();

        // Thêm stem
        if (!string.IsNullOrEmpty(q.Stem))
        {
            content.Append(q.Stem);
        }

        // Thêm audio marker - frontend xử lý format <audio>filename</audio>
        if (q.HasAudio)
        {
            var audioFileName = $"{q.QuestionId}.mp3";
            // Frontend xử lý audio với format <audio>...</audio>
            content.Append($" <audio>{audioFileName}</audio> ");
        }

        // Thêm image marker - dùng format <img> để frontend có thể render
        if (q.HasImage)
        {
            // Image sẽ được lưu với tên = QuestionId trong thư mục Images
            // Frontend sẽ cần xử lý để load từ đúng path (tương tự audio)
            var imageFileName = $"{q.QuestionId}.jpg"; // Mặc định .jpg, có thể là .png
            // Frontend có thể cần xử lý path tương tự audio: /EPZ/{folderName}/Images/{imageFileName}
            content.Append($" <img src=\"Images/{imageFileName}\" alt=\"Question {q.QuestionId}\" style=\"max-width: 100%; height: auto;\" /> ");
        }

        // Thêm latex - giữ nguyên format [latex]...[/latex] vì frontend đã hỗ trợ
        if (!string.IsNullOrEmpty(q.LatexContent))
        {
            content.Append($" [latex]{q.LatexContent}[/latex] ");
        }

        // Thêm match columns nếu là match type
        if (q.QuestionType == "match")
        {
            // Thêm marker [matching] để frontend detect và render MatchingQuestion component
            // Marker được thêm sau stem để frontend có thể hiển thị stem trước khi render matching component
            content.Append(" [matching] ");
            
            // Thêm format A: và B: để hiển thị trong QuestionContent (nếu cần)
            // Frontend sẽ parse và hiển thị trong MatchingQuestion component
            content.Append("\n\nA:\n");
            for (int i = 0; i < q.MatchColumnA.Count; i++)
            {
                content.Append($"{i + 1}. {q.MatchColumnA[i]}\n");
            }
            content.Append("\nB:\n");
            for (int i = 0; i < q.MatchColumnB.Count; i++)
            {
                content.Append($"{(char)('a' + i)}. {q.MatchColumnB[i]}\n");
            }
        }

        return content.ToString().Trim();
    }

    /// <summary>
    /// Extract images từ Word document và lưu vào thư mục Images
    /// </summary>
    private async Task ExtractImagesFromWordAsync(string docxPath, string imageDir, List<ExamZipParsedQuestion> questions)
    {
        try
        {
            using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(docxPath, false);
            var mainPart = doc.MainDocumentPart;
            if (mainPart == null) return;

            var imageParts = mainPart.ImageParts.ToList();
            var imageRelationships = mainPart.ImageParts.Select(ip => mainPart.GetIdOfPart(ip)).ToList();

            // Lấy danh sách các câu hỏi có image
            var questionsWithImages = questions.Where(q => q.HasImage).ToList();

            // Extract images theo thứ tự xuất hiện trong document
            for (int i = 0; i < imageParts.Count && i < questionsWithImages.Count; i++)
            {
                var question = questionsWithImages[i];
                var imagePart = imageParts[i];
                var extension = GetImageExtension(imagePart.ContentType);
                var imageFileName = $"{question.QuestionId}{extension}";
                var imagePath = Path.Combine(imageDir, imageFileName);

                using var imageStream = imagePart.GetStream();
                using var fileStream = File.Create(imagePath);
                await imageStream.CopyToAsync(fileStream);

                _logger.LogInformation("Extracted image: {ImageFileName} for question {QuestionId}", imageFileName, question.QuestionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể extract images từ Word document");
        }
    }

    /// <summary>
    /// Lấy extension từ MIME type
    /// </summary>
    private string GetImageExtension(string contentType)
    {
        return contentType.ToLower() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            _ => ".jpg"
        };
    }

    /// <summary>
    /// Tạo KeyValueList từ database sau khi import xong
    /// Format: (QuestionId:AnswerId);(QuestionId:AnswerId);...
    /// Giống với EPZ import để đảm bảo tính nhất quán
    /// </summary>
    private async Task UpdateKeyValueListAsync(int originalExamPaperId)
    {
        try
        {
            // Lấy tất cả questions và answers đã lưu
            var questions = await _originalExamPaperDetailRepository.GetQueryable()
                .Where(d => d.OriginalExamPaperId == originalExamPaperId)
                .Include(d => d.Answers)
                .ToListAsync();

            var keyValuePairs = new List<string>();

            foreach (var question in questions)
            {
                // Tìm đáp án đúng (IsCorrect = true)
                var correctAnswer = question.Answers?.FirstOrDefault(a => a.IsCorrect);
                
                if (correctAnswer != null)
                {
                    // Format: (QuestionId:AnswerId)
                    keyValuePairs.Add($"({question.OriginalExamPaperDetailId}:{correctAnswer.AnswerId})");
                }
            }

            // Tạo KeyValueList string
            if (keyValuePairs.Any())
            {
                var keyValueList = string.Join(";", keyValuePairs) + ";";
                
                // Cập nhật OriginalExamPaper
                var originalExamPaper = await _originalExamPaperRepository.GetQueryable()
                    .FirstOrDefaultAsync(o => o.OriginalExamPaperId == originalExamPaperId);
                
                if (originalExamPaper != null)
                {
                    originalExamPaper.KeyValueList = keyValueList;
                    await _originalExamPaperRepository.UpdateAsync(originalExamPaper);
                    _logger.LogInformation("Đã tạo KeyValueList cho đề thi {OriginalExamPaperCore}: {KeyValueList}", 
                        originalExamPaper.OriginalExamPaperCore, keyValueList);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo KeyValueList cho đề thi {OriginalExamPaperId}", originalExamPaperId);
            // Không throw exception để không làm gián đoạn quá trình import
        }
    }
}

