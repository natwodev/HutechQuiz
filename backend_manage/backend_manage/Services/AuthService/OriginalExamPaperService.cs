using System.Threading.Tasks;
using backend_manage.DTOs.EPZ;
using backend_manage.Services.Interfaces;
using backend_manage.Data;
using System.Xml.Serialization;
using System.IO;
using backend_manage.Entities;
using backend_manage.Repositories.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO.Compression;
using backend_manage.Hubs;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Http.Features;
using System.Security.Claims;
using System.Collections.Generic;
using backend_manage.DTOs;
using AutoMapper;
using System.Text.Json;

namespace backend_manage.Services.AuthService
{
    public class OriginalExamPaperService : IOriginalExamPaperService
    {
        private readonly IRepository<Subject> _subjectRepository;
        private readonly IRepository<OriginalExamPaper> _originalExamPaperRepository;
        private readonly IRepository<Chapter> _chapterRepository;
        private readonly IRepository<OriginalExamPaperDetail> _originalExamPaperDetailRepository;
        private readonly IRepository<ShuffledExamPaper> _shuffledExamPaperRepository;
        private readonly IRepository<ShuffledExamPaperDetail> _shuffledExamPaperDetailRepository;
        private readonly IRepository<ExamSessionSubject> _examSessionSubjectRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;

        public OriginalExamPaperService(
            IRepository<Subject> subjectRepository,
            IRepository<OriginalExamPaper> originalExamPaperRepository,
            IRepository<Chapter> chapterRepository,
            IRepository<OriginalExamPaperDetail> originalExamPaperDetailRepository,
            IHttpContextAccessor httpContextAccessor,
            IRepository<ShuffledExamPaper> shuffledExamPaperRepository,
            IRepository<ShuffledExamPaperDetail> shuffledExamPaperDetailRepository,
            IRepository<ExamSessionSubject> examSessionSubjectRepository,
            IMapper mapper)
        {
            _subjectRepository = subjectRepository;
            _originalExamPaperRepository = originalExamPaperRepository;
            _chapterRepository = chapterRepository;
            _originalExamPaperDetailRepository = originalExamPaperDetailRepository;
            _httpContextAccessor = httpContextAccessor;
            _shuffledExamPaperRepository = shuffledExamPaperRepository;
            _shuffledExamPaperDetailRepository = shuffledExamPaperDetailRepository;
            _examSessionSubjectRepository = examSessionSubjectRepository;
            _mapper = mapper;
        }

        // Pass giải nén file XML
        public const string ExtractPassword =
            "649224E2-F0AC-42B1-AD1B-2EAF04E2AC7D-FE602240-7E60-43BF-828D-D6AF38A70429-52572FD1-BB94-45AD-95CF-7B2B5C2E85A6-1B3D4CCF-808E-4ABF-8F9E-73ADF041C78B";

        private async Task<string> ExtractAndReadXmlAsync(IFormFile file, string originalExamPaperCore)
        {
            var epzFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "EPZ");
            var epzFilePath = Path.Combine(epzFolder, $"{originalExamPaperCore}.epz");
            var zipFilePath = Path.Combine(epzFolder, $"{originalExamPaperCore}.zip");
            if (File.Exists(epzFilePath) || File.Exists(zipFilePath))
                throw new Exception($"Đã tồn tại file đề với mã '{originalExamPaperCore}' trong hệ thống.");
            if (!Directory.Exists(epzFolder))
                Directory.CreateDirectory(epzFolder);
            using (var fileStream = new FileStream(epzFilePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            if (File.Exists(zipFilePath)) File.Delete(zipFilePath);
            File.Move(epzFilePath, zipFilePath);
            var extractFolder = Path.Combine(epzFolder, originalExamPaperCore);
            if (!Directory.Exists(extractFolder)) Directory.CreateDirectory(extractFolder);
            using (var zipStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read))
                using (var zipFile = new ICSharpCode.SharpZipLib.Zip.ZipFile(zipStream))
                {
                zipFile.Password = ExtractPassword;
                    foreach (ZipEntry entry in zipFile)
                    {
                    if (!entry.IsFile) continue;
                    var entryPath = Path.Combine(extractFolder, entry.Name.Replace("\\", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString()));
                    var entryDir = Path.GetDirectoryName(entryPath);
                    if (!Directory.Exists(entryDir)) Directory.CreateDirectory(entryDir);
                            using (var entryStream = zipFile.GetInputStream(entry))
                    using (var outFileStream = File.Create(entryPath))
                    {
                        await entryStream.CopyToAsync(outFileStream);
                    }
                }
            }
            
            // Xóa file zip sau khi giải nén thành công
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }
            
            var xmlFile = Directory.GetFiles(extractFolder, "*.xml", SearchOption.AllDirectories).FirstOrDefault();
            if (xmlFile == null) throw new Exception("Không tìm thấy file XML trong archive");
            return await File.ReadAllTextAsync(xmlFile);
        }

        private async Task<Subject> EnsureSubjectAsync(MonHocDto monHoc, string userId, DateTime now)
        {
            var exists = await _subjectRepository.GetQueryable().AnyAsync(s => s.SubjectCore == monHoc.MaSoMonHoc);
            if (!exists)
            {
                var subject = new Subject
                {
                    SubjectCore = monHoc.MaSoMonHoc,
                    SubjectName = monHoc.TenMonHoc,
                    DepartmentId = null,
                    CreatedBy = userId,
                    CreatedAt = now
                };
                await _subjectRepository.AddAsync(subject);
                return subject;
            }
            else
            {
                return await _subjectRepository.GetQueryable().FirstOrDefaultAsync(s => s.SubjectCore == monHoc.MaSoMonHoc);
            }
        }

        public async Task ImportFromXmlAsync(IFormFile file, string originalExamPaperCore)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ hoặc rỗng");
            if (!file.FileName.EndsWith(".epz", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("File phải có đuôi .epz");

            // Kiểm tra xem OriginalExamPaperCore đã tồn tại trong database chưa
            var existingExamPaper = await _originalExamPaperRepository.GetQueryable()
                .AnyAsync(x => x.OriginalExamPaperCore == originalExamPaperCore);
            if (existingExamPaper)
                throw new Exception($"Đã tồn tại đề thi với mã '{originalExamPaperCore}' trong hệ thống.");

            // Tách phần giải nén và đọc XML
            string xmlContent = await ExtractAndReadXmlAsync(file, originalExamPaperCore);

            // Deserialize XML
            var serializer = new XmlSerializer(typeof(EPZDto));
            EPZDto epz;
            using (var reader = new StringReader(xmlContent))
            {
                epz = (EPZDto)serializer.Deserialize(reader);
            }

            var monHoc = epz.MonHoc;
            if (monHoc == null) throw new Exception("XML không hợp lệ: thiếu MonHoc");
            var now = DateTimeHelper.GetVietnamTime();
            var userIdForExamPaper = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdForExamPaper))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo đề thi gốc.");

            // Tách phần tạo/lấy Subject
            var subject = await EnsureSubjectAsync(monHoc, userIdForExamPaper, now);

            // Lấy tên file epz (không bao gồm đuôi mở rộng) để làm Title
            var epzFileName = Path.GetFileNameWithoutExtension(file.FileName);

            // Save OriginalExamPaper (phải tạo trước để lấy Id cho detail)
            var originalExamPaper = new OriginalExamPaper
            {
                Title = epzFileName, // Lấy tên file epz làm tiêu đề đề thi
                Description = null, // TODO: Bổ sung nếu có trường mô tả trong XML
                SubjectId = subject.SubjectId,
                CreatedBy = userIdForExamPaper,
                CreatedAt = now,
                DurationMinutes = 0, // TODO: Bổ sung nếu có trường thời gian làm bài trong XML
                TotalQuestions = monHoc.TongSoCauLay > 0 ? monHoc.TongSoCauLay : 0,
                OriginalExamPaperCore = originalExamPaperCore, // <-- cập nhật ở đây
                IsApproved = true // Đề thi gốc mặc định được duyệt khi thêm mới
            };
            await _originalExamPaperRepository.AddAsync(originalExamPaper);

            // Lưu các phần (section) dựa vào TenPhan
            if (monHoc.Phan != null)
            {
                // Map để xử lý parent question và child question
                var questionIdMap = new Dictionary<string, int>();
                int globalOrder = 1; // Order chung cho toàn bộ đề thi
                
                foreach (var phan in monHoc.Phan)
                {
                    int? parentChapterId = null;
                    if (!string.IsNullOrEmpty(phan.MaPhanCha) &&
                        phan.MaPhanCha != "00000000-0000-0000-0000-000000000000")
                    {
                        var parentPhan = monHoc.Phan.FirstOrDefault(p => p.MaPhan == phan.MaPhanCha);
                        if (parentPhan != null)
                        {
                            var parentChapter = await _chapterRepository.GetQueryable()
                                .FirstOrDefaultAsync(c =>
                                    c.Name == parentPhan.TenPhan && c.SubjectId == subject.SubjectId);
                            if (parentChapter != null)
                            {
                                parentChapterId = parentChapter.ChapterId;
                            }
                        }
                    }

                    var chapter = await _chapterRepository.GetQueryable()
                        .FirstOrDefaultAsync(c => c.Name == phan.TenPhan && c.SubjectId == subject.SubjectId);
                    
                    // Kiểm tra xem chapter này có câu hỏi nhóm không
                    bool hasGroupQuestions = phan.CauHoi?.Any(c => c.SoCauHoiCon > 0) ?? false;
                    
                    if (chapter == null)
                    {
                        chapter = new Chapter
                        {
                            Name = phan.TenPhan,
                            Description = phan.NoiDung,
                            SubjectId = subject.SubjectId,
                            CreatedBy = userIdForExamPaper,
                            CreatedAt = now,
                            Order = 0,
                            IsGroupQuestion = hasGroupQuestions,
                            ParentChapterId = parentChapterId
                        };
                        await _chapterRepository.AddAsync(chapter);
                    }
                    else
                    {
                        // Cập nhật IsGroupQuestion nếu cần
                        if (chapter.IsGroupQuestion != hasGroupQuestions)
                        {
                            chapter.IsGroupQuestion = hasGroupQuestions;
                            await _chapterRepository.UpdateAsync(chapter);
                        }
                    }

                    if (phan.CauHoi != null)
                    {
                        
                        // Đầu tiên, lưu tất cả câu hỏi cha (parent questions)
                        var parentQuestions = phan.CauHoi.Where(c => c.SoCauHoiCon > 0).ToList();
                        foreach (var parentCauHoi in parentQuestions)
                        {
                            var detail = new OriginalExamPaperDetail
                            {
                                OriginalExamPaperId = originalExamPaper.OriginalExamPaperId,
                                ChapterId = chapter.ChapterId,
                                Order = globalOrder++,
                                QuestionContent = parentCauHoi.NoiDung,
                                Answer1 = null, // Câu hỏi cha không có đáp án
                                Answer2 = null,
                                Answer3 = null,
                                Answer4 = null,
                                CorrectAnswerIndex = null,
                                CanShuffleQuestion = parentCauHoi.HoanVi,
                                AnswerShuffleInfo = null,
                                ParentQuestionId = null, // Câu hỏi cha không có parent
                                CreatedBy = userIdForExamPaper,
                                CreatedAt = now
                            };
                            await _originalExamPaperDetailRepository.AddAsync(detail);
                            questionIdMap[parentCauHoi.MaCauHoi] = detail.OriginalExamPaperDetailId;
                        }
                        
                        // Sau đó, lưu tất cả câu hỏi con (child questions)
                        var childQuestions = phan.CauHoi.Where(c => !string.IsNullOrEmpty(c.MaCauHoiCha) && 
                                                                     c.MaCauHoiCha != "00000000-0000-0000-0000-000000000000").ToList();
                        foreach (var childCauHoi in childQuestions)
                        {
                            var answers = childCauHoi.CauTraLoi?.OrderBy(a => a.ThuTu).ToList() ??
                                          new List<DTOs.EPZ.CauTraLoiDto>();
                            
                            // Xử lý thông tin hoán vị từ XML
                            bool canShuffleQuestion = childCauHoi.HoanVi;
                            
                            // Tạo thông tin hoán vị đáp án dạng JSON
                            string answerShuffleInfo = null;
                            if (answers.Any())
                            {
                                var shuffleInfo = new Dictionary<string, bool>();
                                for (int i = 0; i < answers.Count; i++)
                                {
                                    shuffleInfo[(i + 1).ToString()] = answers[i].HoanVi;
                                }
                                answerShuffleInfo = JsonSerializer.Serialize(shuffleInfo);
                            }
                            
                            var detail = new OriginalExamPaperDetail
                            {
                                OriginalExamPaperId = originalExamPaper.OriginalExamPaperId,
                                ChapterId = chapter.ChapterId,
                                Order = globalOrder++,
                                QuestionContent = childCauHoi.NoiDung,
                                Answer1 = answers.Count > 0 ? answers[0].NoiDung : null,
                                Answer2 = answers.Count > 1 ? answers[1].NoiDung : null,
                                Answer3 = answers.Count > 2 ? answers[2].NoiDung : null,
                                Answer4 = answers.Count > 3 ? answers[3].NoiDung : null,
                                CorrectAnswerIndex = answers.FindIndex(a => a.LaDapAn) >= 0
                                    ? answers.FindIndex(a => a.LaDapAn) + 1
                                    : null,
                                CanShuffleQuestion = canShuffleQuestion,
                                AnswerShuffleInfo = answerShuffleInfo,
                                ParentQuestionId = questionIdMap.ContainsKey(childCauHoi.MaCauHoiCha) 
                                    ? questionIdMap[childCauHoi.MaCauHoiCha] 
                                    : null,
                                CreatedBy = userIdForExamPaper,
                                CreatedAt = now
                            };
                            await _originalExamPaperDetailRepository.AddAsync(detail);
                            questionIdMap[childCauHoi.MaCauHoi] = detail.OriginalExamPaperDetailId;
                        }
                        
                        // Cuối cùng, lưu các câu hỏi độc lập (không phải cha cũng không phải con)
                        var independentQuestions = phan.CauHoi.Where(c => c.SoCauHoiCon == 0 && 
                                                                         (string.IsNullOrEmpty(c.MaCauHoiCha) || 
                                                                          c.MaCauHoiCha == "00000000-0000-0000-0000-000000000000")).ToList();
                        foreach (var independentCauHoi in independentQuestions)
                        {
                            var answers = independentCauHoi.CauTraLoi?.OrderBy(a => a.ThuTu).ToList() ??
                                          new List<DTOs.EPZ.CauTraLoiDto>();
                            
                            // Xử lý thông tin hoán vị từ XML
                            bool canShuffleQuestion = independentCauHoi.HoanVi;
                            
                            // Tạo thông tin hoán vị đáp án dạng JSON
                            string answerShuffleInfo = null;
                            if (answers.Any())
                            {
                                var shuffleInfo = new Dictionary<string, bool>();
                                for (int i = 0; i < answers.Count; i++)
                                {
                                    shuffleInfo[(i + 1).ToString()] = answers[i].HoanVi;
                                }
                                answerShuffleInfo = JsonSerializer.Serialize(shuffleInfo);
                            }
                            
                            var detail = new OriginalExamPaperDetail
                            {
                                OriginalExamPaperId = originalExamPaper.OriginalExamPaperId,
                                ChapterId = chapter.ChapterId,
                                Order = globalOrder++,
                                QuestionContent = independentCauHoi.NoiDung,
                                Answer1 = answers.Count > 0 ? answers[0].NoiDung : null,
                                Answer2 = answers.Count > 1 ? answers[1].NoiDung : null,
                                Answer3 = answers.Count > 2 ? answers[2].NoiDung : null,
                                Answer4 = answers.Count > 3 ? answers[3].NoiDung : null,
                                CorrectAnswerIndex = answers.FindIndex(a => a.LaDapAn) >= 0
                                    ? answers.FindIndex(a => a.LaDapAn) + 1
                                    : null,
                                CanShuffleQuestion = canShuffleQuestion,
                                AnswerShuffleInfo = answerShuffleInfo,
                                ParentQuestionId = null, // Câu hỏi độc lập không có parent
                                CreatedBy = userIdForExamPaper,
                                CreatedAt = now
                            };
                            await _originalExamPaperDetailRepository.AddAsync(detail);
                            questionIdMap[independentCauHoi.MaCauHoi] = detail.OriginalExamPaperDetailId;
                        }
                    }
                }
            }
        }
        
        public async Task CreateShuffledExamPapersAsync(string originalExamPaperCore, int count)
        {
            if (string.IsNullOrWhiteSpace(originalExamPaperCore))
                throw new ArgumentException("Mã đề thi gốc không hợp lệ");
            if (count <= 0)
                throw new ArgumentException("Số lượng đề hoán vị phải lớn hơn 0");

            var originalExamPaper = await _originalExamPaperRepository.GetQueryable()
                .Include(o => o.OriginalExamPaperDetails)
                .FirstOrDefaultAsync(o => o.OriginalExamPaperCore == originalExamPaperCore);
            if (originalExamPaper == null)
                throw new Exception($"Không tìm thấy đề thi gốc với mã '{originalExamPaperCore}'");

            var subjectId = originalExamPaper.SubjectId;
            var now = DateTimeHelper.GetVietnamTime();
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo đề hoán vị.");

            for (int i = 0; i < count; i++)
            {
                int nextIndex = originalExamPaper.TotalShuffledPapers + 1 + i;
                string shuffledExamPaperCore = $"{originalExamPaper.OriginalExamPaperCore}HV{nextIndex}";
                var shuffledExamPaper = new ShuffledExamPaper
                {
                    Title = $"{originalExamPaper.Title} - Hoán vị {nextIndex}",
                    ShuffledExamPaperCore = shuffledExamPaperCore,
                    OriginalExamPaperId = originalExamPaper.OriginalExamPaperId,
                    SubjectId = subjectId,
                    IsApproved = true, // Đề hoán vị mặc định được duyệt khi tạo mới
                    CreatedBy = userId,
                    CreatedAt = now,
                    TotalUsageCount = 0
                };
                await _shuffledExamPaperRepository.AddAsync(shuffledExamPaper);

                // Tạo detail cho đề hoán vị này
                var originalDetails = originalExamPaper.OriginalExamPaperDetails.OrderBy(d => d.Order).ToList();
                Console.WriteLine($"Tạo đề hoán vị {shuffledExamPaperCore} với {originalDetails.Count} câu hỏi gốc");
                var random = new Random();
                
                // Phân loại câu hỏi
                var parentQuestions = originalDetails.Where(q => q.ParentQuestionId == null && 
                                                               originalDetails.Any(c => c.ParentQuestionId == q.OriginalExamPaperDetailId)).ToList();
                var childQuestions = originalDetails.Where(q => q.ParentQuestionId.HasValue).ToList();
                var independentQuestions = originalDetails.Where(q => q.ParentQuestionId == null && 
                                                                   !originalDetails.Any(c => c.ParentQuestionId == q.OriginalExamPaperDetailId)).ToList();
                
                // Tạo danh sách câu hỏi với thứ tự ban đầu
                var shuffledQuestions = new List<OriginalExamPaperDetail>();
                
                // Phân loại câu hỏi độc lập theo khả năng hoán vị
                var shuffleableIndependentQuestions = independentQuestions.Where(q => q.CanShuffleQuestion).ToList();
                var nonShuffleableIndependentQuestions = independentQuestions.Where(q => !q.CanShuffleQuestion).ToList();
                

                
                // Tạo danh sách câu hỏi cuối cùng theo thứ tự ban đầu của đề thi gốc
                // Giữ nguyên thứ tự ban đầu, chỉ hoán vị những câu hỏi độc lập có thể hoán vị
                var allQuestions = originalDetails.OrderBy(q => q.Order).ToList();
                
                // Tạo mapping cho câu hỏi độc lập có thể hoán vị
                var questionMapping = new Dictionary<int, OriginalExamPaperDetail>();
                if (shuffleableIndependentQuestions.Count >= 2)
                {
                    var shuffledShuffleableQuestions = shuffleableIndependentQuestions.OrderBy(x => random.Next()).ToList();
                    for (int mappingIndex = 0; mappingIndex < shuffleableIndependentQuestions.Count; mappingIndex++)
                    {
                        questionMapping[shuffleableIndependentQuestions[mappingIndex].OriginalExamPaperDetailId] = shuffledShuffleableQuestions[mappingIndex];
                    }
                }
                
                // Tạo danh sách câu hỏi cuối cùng
                foreach (var question in allQuestions)
                {
                    if (question.ParentQuestionId == null && 
                        !originalDetails.Any(c => c.ParentQuestionId == question.OriginalExamPaperDetailId))
                    {
                        // Đây là câu hỏi độc lập
                        if (question.CanShuffleQuestion && questionMapping.ContainsKey(question.OriginalExamPaperDetailId))
                        {
                            // Sử dụng câu hỏi đã hoán vị
                            shuffledQuestions.Add(questionMapping[question.OriginalExamPaperDetailId]);
                        }
                        else
                        {
                            // Giữ nguyên vị trí
                            shuffledQuestions.Add(question);
                        }
                    }
                    else if (question.ParentQuestionId == null && 
                             originalDetails.Any(c => c.ParentQuestionId == question.OriginalExamPaperDetailId))
                    {
                        // Đây là câu hỏi cha - giữ nguyên vị trí
                        shuffledQuestions.Add(question);
                        
                        // Thêm câu hỏi con ngay sau
                        var childrenOfParent = childQuestions.Where(c => c.ParentQuestionId == question.OriginalExamPaperDetailId)
                                                           .OrderBy(c => c.Order).ToList();
                        shuffledQuestions.AddRange(childrenOfParent);
                    }
                    else if (question.ParentQuestionId.HasValue)
                    {
                        // Đây là câu hỏi con - đã được thêm ở trên, bỏ qua
                        continue;
                    }
                }
                
                // Đảm bảo không có trùng lặp câu hỏi và giữ đúng thứ tự
                var seenIds = new HashSet<int>();
                var finalShuffledQuestions = new List<OriginalExamPaperDetail>();
                
                foreach (var question in shuffledQuestions)
                {
                    if (!seenIds.Contains(question.OriginalExamPaperDetailId))
                    {
                        seenIds.Add(question.OriginalExamPaperDetailId);
                        finalShuffledQuestions.Add(question);
                    }
                }
                
                shuffledQuestions = finalShuffledQuestions;
                
                Console.WriteLine($"Đề hoán vị {shuffledExamPaperCore} có {shuffledQuestions.Count} câu hỏi sau khi loại bỏ trùng lặp");
                
                var answerKeyParts = new List<string>();
                int order = 1;
                
                // Mapping để theo dõi OriginalExamPaperDetailId -> ShuffledExamPaperDetailId
                var originalToShuffledMapping = new Dictionary<int, int>();
                
                foreach (var question in shuffledQuestions)
                {
                    // Xử lý hoán vị đáp án dựa trên thông tin từ JSON
                    Dictionary<string, bool> shuffleInfo = null;
                    
                    if (!string.IsNullOrEmpty(question.AnswerShuffleInfo))
                    {
                        try
                        {
                            shuffleInfo = JsonSerializer.Deserialize<Dictionary<string, bool>>(question.AnswerShuffleInfo);
                        }
                        catch
                        {
                            shuffleInfo = null;
                        }
                    }
                    
                    // Tạo thứ tự đáp án mới
                    List<int> newAnswerOrder = new List<int> { 1, 2, 3, 4 };
                    
                    if (shuffleInfo != null && shuffleInfo.Any())
                    {
                        // Xác định các vị trí đáp án cố định và có thể hoán vị
                        var fixedPositions = new List<int>(); // index 1-based
                        var shuffleablePositions = new List<int>();
                        for (int j = 1; j <= 4; j++)
                        {
                            if (shuffleInfo.ContainsKey(j.ToString()) && shuffleInfo[j.ToString()])
                                shuffleablePositions.Add(j);
                            else
                                fixedPositions.Add(j);
                        }
                        
                        // Chỉ hoán vị nếu có ít nhất 2 đáp án có thể hoán vị
                        if (shuffleablePositions.Count >= 2)
                        {
                            // Tạo một hoán vị ngẫu nhiên của các vị trí có thể hoán vị
                            var shuffledPositions = shuffleablePositions.OrderBy(x => random.Next()).ToList();
                            
                            // Tạo thứ tự mới
                            newAnswerOrder.Clear();
                            int shuffleIdx = 0;
                            
                            for (int j = 1; j <= 4; j++)
                            {
                                if (fixedPositions.Contains(j))
                                {
                                    newAnswerOrder.Add(j); // giữ nguyên vị trí
                                }
                                else
                                {
                                    newAnswerOrder.Add(shuffledPositions[shuffleIdx++]);
                                }
                            }
                        }
                        // Nếu không có đủ đáp án để hoán vị, giữ nguyên thứ tự ban đầu
                    }
                    
                    string answerOrder = string.Join("", newAnswerOrder);

                    string?[] answers = { question.Answer1, question.Answer2, question.Answer3, question.Answer4 };
                    string?[] shuffledAnswers = new string?[4];
                    
                    // Tạo đáp án theo thứ tự mới
                    for (int k = 0; k < 4; k++)
                    {
                        shuffledAnswers[k] = answers[newAnswerOrder[k] - 1];
                    }
                    
                    // Tính toán đáp án đúng mới
                    int? correctIndex = question.CorrectAnswerIndex.HasValue ? 
                        newAnswerOrder.IndexOf(question.CorrectAnswerIndex.Value) + 1 : 
                        (int?)null;

                    // Xác định ký tự đáp án đúng (A/B/C/D)
                    string correctChar = correctIndex.HasValue && correctIndex.Value >= 1 && correctIndex.Value <= 4
                        ? ((char)('A' + correctIndex.Value - 1)).ToString()
                        : "-";
                    answerKeyParts.Add($"({order},{correctChar})");

                    // Tìm ParentQuestionId cho câu hỏi con
                    int? parentQuestionId = null;
                    if (question.ParentQuestionId.HasValue)
                    {
                        // Sử dụng mapping để tìm ShuffledExamPaperDetailId của câu hỏi cha
                        if (originalToShuffledMapping.ContainsKey(question.ParentQuestionId.Value))
                        {
                            parentQuestionId = originalToShuffledMapping[question.ParentQuestionId.Value];
                        }
                    }
                    
                    var shuffledDetail = new ShuffledExamPaperDetail
                    {
                        ShuffledExamPaperId = shuffledExamPaper.ShuffledExamPaperId,
                        Order = order++,
                        AnswerOrder = answerOrder,
                        OriginalExamPaperDetailId = question.OriginalExamPaperDetailId,
                        ParentQuestionId = parentQuestionId,
                        CreatedBy = userId,
                        CreatedAt = now
                    };
                    await _shuffledExamPaperDetailRepository.AddAsync(shuffledDetail);
                    
                    // Lưu mapping
                    originalToShuffledMapping[question.OriginalExamPaperDetailId] = shuffledDetail.ShuffledExamPaperDetailId;
                }
                // Lưu AnswerKey vào đề hoán vị
                shuffledExamPaper.AnswerKey = string.Join(";", answerKeyParts) + ";";
                await _shuffledExamPaperRepository.UpdateAsync(shuffledExamPaper);
            }
            originalExamPaper.TotalShuffledPapers += count;
            await _originalExamPaperRepository.UpdateAsync(originalExamPaper);
        }

        public async Task<OriginalExamPaperDto> GetWithDetailsAsync(string originalExamPaperCore)
        {
            var examPaper = await _originalExamPaperRepository.GetQueryable()
                .Where(x => x.OriginalExamPaperCore == originalExamPaperCore)
                .Include(x => x.OriginalExamPaperDetails)
                .ThenInclude(d => d.ChildQuestions)
                .FirstOrDefaultAsync();
            if (examPaper == null) return null;
            return _mapper.Map<OriginalExamPaperDto>(examPaper);
        }
    }
}