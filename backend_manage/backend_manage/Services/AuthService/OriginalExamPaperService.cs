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
                OriginalExamPaperCore = originalExamPaperCore // <-- cập nhật ở đây
            };
            await _originalExamPaperRepository.AddAsync(originalExamPaper);

            // Lưu các phần (section) dựa vào TenPhan
            if (monHoc.Phan != null)
            {
                // Map tạm để xử lý parent question
                var questionIdMap = new Dictionary<string, int>();
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
                            IsGroupQuestion = false,
                            ParentChapterId = parentChapterId
                        };
                        await _chapterRepository.AddAsync(chapter);
                    }

                    if (phan.CauHoi != null)
                    {
                        int order = 1;
                        // Lưu tạm mapping MaCauHoi -> OriginalExamPaperDetailId
                        var tempDetails = new List<(string MaCauHoi, OriginalExamPaperDetail Detail)>();
                        foreach (var cauHoi in phan.CauHoi)
                        {
                            var answers = cauHoi.CauTraLoi?.OrderBy(a => a.ThuTu).ToList() ??
                                          new List<DTOs.EPZ.CauTraLoiDto>();
                            
                            // Xử lý thông tin hoán vị từ XML
                            bool canShuffleQuestion = cauHoi.HoanVi;
                            
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
                                Order = order,
                                QuestionContent = cauHoi.NoiDung,
                                Answer1 = answers.Count > 0 ? answers[0].NoiDung : null,
                                Answer2 = answers.Count > 1 ? answers[1].NoiDung : null,
                                Answer3 = answers.Count > 2 ? answers[2].NoiDung : null,
                                Answer4 = answers.Count > 3 ? answers[3].NoiDung : null,
                                CorrectAnswerIndex = answers.FindIndex(a => a.LaDapAn) >= 0
                                    ? answers.FindIndex(a => a.LaDapAn) + 1
                                    : null,
                                CanShuffleQuestion = canShuffleQuestion,
                                AnswerShuffleInfo = answerShuffleInfo,
                                CreatedBy = userIdForExamPaper,
                                CreatedAt = now,
                                // ParentQuestionId sẽ gán sau khi đã có mapping
                            };
                            await _originalExamPaperDetailRepository.AddAsync(detail);
                            tempDetails.Add((cauHoi.MaCauHoi, detail));
                            order++;
                        }

                        // Sau khi lưu xong, cập nhật ParentQuestionId nếu có
                        foreach (var (MaCauHoi, Detail) in tempDetails)
                        {
                            var cauHoi = phan.CauHoi.FirstOrDefault(c => c.MaCauHoi == MaCauHoi);
                            if (cauHoi != null && !string.IsNullOrEmpty(cauHoi.MaCauHoiCha) &&
                                cauHoi.MaCauHoiCha != "00000000-0000-0000-0000-000000000000")
                            {
                                var parent = tempDetails.FirstOrDefault(t => t.MaCauHoi == cauHoi.MaCauHoiCha).Detail;
                                if (parent != null)
                                {
                                    Detail.ParentQuestionId = parent.OriginalExamPaperDetailId;
                                    await _originalExamPaperDetailRepository
                                        .AddAsync(Detail); // hoặc update nếu repo có hàm update
                                }
                            }
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
                    ExamSessionSubjectId = null,
                    SubjectId = subjectId,
                    IsApproved = false,
                    CreatedBy = userId,
                    CreatedAt = now,
                    TotalUsageCount = 0
                };
                await _shuffledExamPaperRepository.AddAsync(shuffledExamPaper);

                // Tạo detail cho đề hoán vị này
                var originalDetails = originalExamPaper.OriginalExamPaperDetails.ToList();
                var random = new Random();
                
                // Tạo danh sách câu hỏi với thứ tự ban đầu
                var shuffledQuestions = originalDetails.ToList();
                
                // Hoán vị câu hỏi có thể hoán vị với nhau
                var shuffleableQuestions = shuffledQuestions.Where(q => q.CanShuffleQuestion).ToList();
                var nonShuffleableQuestions = shuffledQuestions.Where(q => !q.CanShuffleQuestion).ToList();
                
                // Hoán vị chỉ những câu hỏi có thể hoán vị
                var shuffledShuffleableQuestions = shuffleableQuestions.OrderBy(x => random.Next()).ToList();
                
                // Tạo lại danh sách với thứ tự đúng
                shuffledQuestions.Clear();
                int shuffleableIndex = 0;
                int nonShuffleableIndex = 0;
                
                foreach (var question in originalDetails)
                {
                    if (question.CanShuffleQuestion)
                    {
                        shuffledQuestions.Add(shuffledShuffleableQuestions[shuffleableIndex++]);
                    }
                    else
                    {
                        shuffledQuestions.Add(nonShuffleableQuestions[nonShuffleableIndex++]);
                    }
                }
                
                var answerKeyParts = new List<string>();
                int order = 1;
                foreach (var question in shuffledQuestions)
                {
                    string answerOrder = "1234"; // Mặc định không hoán vị đáp án
                    List<int> answerIndexes = new List<int> { 1, 2, 3, 4 };
                    
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
                    
                    if (shuffleInfo != null)
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
                        // Lấy đáp án có thể hoán vị
                        var shuffleableAnswers = shuffleablePositions.ToList();
                        // Xáo trộn các đáp án có thể hoán vị
                        if (shuffleableAnswers.Count > 1)
                            shuffleableAnswers = shuffleableAnswers.OrderBy(x => random.Next()).ToList();
                        // Gán lại thứ tự mới: đáp án cố định giữ nguyên vị trí, đáp án hoán vị gán vào các vị trí còn lại
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
                                newAnswerOrder.Add(shuffleableAnswers[shuffleIdx++]);
                            }
                        }
                    }
                    
                    answerOrder = string.Join("", newAnswerOrder);
                    answerIndexes = newAnswerOrder;

                    string?[] answers = { question.Answer1, question.Answer2, question.Answer3, question.Answer4 };
                    string?[] shuffledAnswers = new string?[4];
                    
                    // Tạo đáp án theo thứ tự mới
                    for (int j = 0; j < 4; j++)
                    {
                        shuffledAnswers[j] = answers[answerIndexes[j] - 1];
                    }
                    
                    // Tính toán đáp án đúng mới
                    int? correctIndex = question.CorrectAnswerIndex.HasValue ? 
                        answerIndexes.IndexOf(question.CorrectAnswerIndex.Value) + 1 : 
                        (int?)null;

                    // Xác định ký tự đáp án đúng (A/B/C/D)
                    string correctChar = correctIndex.HasValue && correctIndex.Value >= 1 && correctIndex.Value <= 4
                        ? ((char)('A' + correctIndex.Value - 1)).ToString()
                        : "-";
                    answerKeyParts.Add($"({order},{correctChar})");

                    var shuffledDetail = new ShuffledExamPaperDetail
                    {
                        ShuffledExamPaperId = shuffledExamPaper.ShuffledExamPaperId,
                        Order = order++,
                        AnswerOrder = answerOrder,
                        OriginalExamPaperDetailId = question.OriginalExamPaperDetailId,
                        ParentQuestionId = null, // Nếu có logic cha-con thì cần xử lý thêm
                        CreatedBy = userId,
                        CreatedAt = now
                    };
                    await _shuffledExamPaperDetailRepository.AddAsync(shuffledDetail);
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