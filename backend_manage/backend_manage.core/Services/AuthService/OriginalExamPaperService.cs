using System.Security.Claims;
using System.Text;
using System.Xml.Serialization;
using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using backend_manage.shared.DTOs.EPZ;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace backend_manage.core.Services.AuthService
{
    public class OriginalExamPaperService : IOriginalExamPaperService
    {
        private readonly IRepository<Subject> _subjectRepository;
        private readonly IRepository<OriginalExamPaper> _originalExamPaperRepository;
        private readonly IRepository<Chapter> _chapterRepository;
        private readonly IRepository<OriginalExamPaperDetail> _originalExamPaperDetailRepository;
        private readonly IRepository<ShuffledExamPaper> _shuffledExamPaperRepository;
        private readonly IRepository<Answers> _answersRepository;
        private readonly IRepository<ExamSessionSubject> _examSessionSubjectRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;

        public OriginalExamPaperService(
            IRepository<Subject> subjectRepository,
            IRepository<OriginalExamPaper> originalExamPaperRepository,
            IRepository<Chapter> chapterRepository,
            IRepository<OriginalExamPaperDetail> originalExamPaperDetailRepository,
            IRepository<Answers> answersRepository,
            IHttpContextAccessor httpContextAccessor,
            IRepository<ShuffledExamPaper> shuffledExamPaperRepository,
            IRepository<ExamSessionSubject> examSessionSubjectRepository,
            IMapper mapper)
        {
            _subjectRepository = subjectRepository;
            _originalExamPaperRepository = originalExamPaperRepository;
            _chapterRepository = chapterRepository;
            _originalExamPaperDetailRepository = originalExamPaperDetailRepository;
            _answersRepository = answersRepository;
            _httpContextAccessor = httpContextAccessor;
            _shuffledExamPaperRepository = shuffledExamPaperRepository;
            _examSessionSubjectRepository = examSessionSubjectRepository;
            _mapper = mapper;
        }

        // Pass giải nén file XML
        private const string ExtractPassword =
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
                using (var zipFile = new ZipFile(zipStream))
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
                OriginalExamPaperCore = originalExamPaperCore,
                IsApproved = true, // Đề thi gốc mặc định được duyệt khi thêm mới
                TotalShuffledPapers = 0
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
                                Order = globalOrder++,
                                QuestionContent = parentCauHoi.NoiDung,
                                CorrectAnswerIndex = null, // Câu hỏi cha không có đáp án đúng
                                ParentQuestionId = null, // Câu hỏi cha không có câu hỏi cha
                                CanShuffleQuestion = parentCauHoi.HoanVi,
                                ChapterId = chapter.ChapterId,
                                CreatedBy = userIdForExamPaper,
                                CreatedAt = now
                            };
                            await _originalExamPaperDetailRepository.AddAsync(detail);
                            questionIdMap[parentCauHoi.MaCauHoi] = detail.OriginalExamPaperDetailId;
                        }
                        
                        // Sau đó, lưu tất cả câu hỏi con (child questions) với order riêng cho từng nhóm
                        var childQuestions = phan.CauHoi.Where(c => !string.IsNullOrEmpty(c.MaCauHoiCha) && 
                                                                    c.MaCauHoiCha != "00000000-0000-0000-0000-000000000000").ToList();
                        
                        // Nhóm câu hỏi con theo câu hỏi cha
                        var childQuestionsByParent = childQuestions.GroupBy(c => c.MaCauHoiCha).ToList();
                        
                        foreach (var parentGroup in childQuestionsByParent)
                        {
                            int childOrder = 1; // Order riêng cho từng nhóm câu hỏi con
                            foreach (var childCauHoi in parentGroup)
                            {
                                var answers = childCauHoi.CauTraLoi?.OrderBy(a => a.ThuTu).ToList() ??
                                              new List<CauTraLoiDto>();
                                
                                // Xử lý thông tin hoán vị từ XML
                                bool canShuffleQuestion = childCauHoi.HoanVi;
                                
                                // Tìm đáp án đúng
                                int? correctAnswerIndex = null;
                                if (answers.Any())
                                {
                                    var correctAnswer = answers.FirstOrDefault(a => a.LaDapAn);
                                    if (correctAnswer != null)
                                    {
                                        correctAnswerIndex = correctAnswer.ThuTu;
                                    }
                                }
                                
                                var detail = new OriginalExamPaperDetail
                                {
                                    OriginalExamPaperId = originalExamPaper.OriginalExamPaperId,
                                    Order = childOrder++,
                                    QuestionContent = childCauHoi.NoiDung,
                                    CorrectAnswerIndex = correctAnswerIndex,
                                    ParentQuestionId = questionIdMap.ContainsKey(childCauHoi.MaCauHoiCha) ? 
                                                      questionIdMap[childCauHoi.MaCauHoiCha] : null,
                                    CanShuffleQuestion = canShuffleQuestion,
                                    ChapterId = chapter.ChapterId,
                                    CreatedBy = userIdForExamPaper,
                                    CreatedAt = now
                                };
                                await _originalExamPaperDetailRepository.AddAsync(detail);
                                questionIdMap[childCauHoi.MaCauHoi] = detail.OriginalExamPaperDetailId;
                                
                                // Lưu các đáp án cho câu hỏi con
                                if (answers.Any())
                                {
                                    foreach (var answer in answers)
                                    {
                                        var answerEntity = new Answers
                                        {
                                            Order = answer.ThuTu,
                                            AnswerContent = answer.NoiDung,
                                            IsCorrect = answer.LaDapAn,
                                            CanShuffleAnswer = answer.HoanVi,
                                            OriginalExamPaperDetailId = detail.OriginalExamPaperDetailId,
                                            CreatedBy = userIdForExamPaper,
                                            CreatedAt = now
                                        };
                                        await _answersRepository.AddAsync(answerEntity);
                                    }
                                }
                            }
                        }
                        
                        // Cuối cùng, lưu các câu hỏi độc lập (không phải cha cũng không phải con)
                        var independentQuestions = phan.CauHoi.Where(c => c.SoCauHoiCon == 0 && 
                                                                         (string.IsNullOrEmpty(c.MaCauHoiCha) || 
                                                                          c.MaCauHoiCha == "00000000-0000-0000-0000-000000000000")).ToList();
                        foreach (var independentCauHoi in independentQuestions)
                        {
                            var answers = independentCauHoi.CauTraLoi?.OrderBy(a => a.ThuTu).ToList() ??
                                          new List<CauTraLoiDto>();
                            
                            // Xử lý thông tin hoán vị từ XML
                            bool canShuffleQuestion = independentCauHoi.HoanVi;
                            
                            // Tìm đáp án đúng
                            int? correctAnswerIndex = null;
                            if (answers.Any())
                            {
                                var correctAnswer = answers.FirstOrDefault(a => a.LaDapAn);
                                if (correctAnswer != null)
                                {
                                    correctAnswerIndex = correctAnswer.ThuTu;
                                }
                            }
                            
                            var detail = new OriginalExamPaperDetail
                            {
                                OriginalExamPaperId = originalExamPaper.OriginalExamPaperId,
                                Order = globalOrder++,
                                QuestionContent = independentCauHoi.NoiDung,
                                CorrectAnswerIndex = correctAnswerIndex,
                                ParentQuestionId = null, // Câu hỏi độc lập không có câu hỏi cha
                                CanShuffleQuestion = canShuffleQuestion,
                                ChapterId = chapter.ChapterId,
                                CreatedBy = userIdForExamPaper,
                                CreatedAt = now
                            };
                            await _originalExamPaperDetailRepository.AddAsync(detail);
                            questionIdMap[independentCauHoi.MaCauHoi] = detail.OriginalExamPaperDetailId;
                            
                            // Lưu các đáp án cho câu hỏi độc lập
                            if (answers.Any())
                            {
                                foreach (var answer in answers)
                                {
                                    var answerEntity = new Answers
                                    {
                                        Order = answer.ThuTu,
                                        AnswerContent = answer.NoiDung,
                                        IsCorrect = answer.LaDapAn,
                                        CanShuffleAnswer = answer.HoanVi,
                                        OriginalExamPaperDetailId = detail.OriginalExamPaperDetailId,
                                        CreatedBy = userIdForExamPaper,
                                        CreatedAt = now
                                    };
                                    await _answersRepository.AddAsync(answerEntity);
                                }
                            }
                        }
                    }
                }
            }
        }
        public async Task<OriginalExamPaperDto> GetWithDetailsAsync(string originalExamPaperCore)
        {
            var examPaper = await _originalExamPaperRepository.GetQueryable()
                .Where(x => x.OriginalExamPaperCore == originalExamPaperCore)
                .Include(x => x.OriginalExamPaperDetails)
                    .ThenInclude(d => d.Answers)
                .Include(x => x.OriginalExamPaperDetails)
                    .ThenInclude(d => d.ChildQuestions)
                        .ThenInclude(c => c.Answers)
                .FirstOrDefaultAsync();
            if (examPaper == null) return null;
            return _mapper.Map<OriginalExamPaperDto>(examPaper);
        }

    }
}