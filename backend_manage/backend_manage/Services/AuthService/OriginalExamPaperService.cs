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
using System.Text;

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
                                    Order = childOrder++, // Sử dụng order riêng cho câu hỏi con
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
                .FirstOrDefaultAsync(o => o.OriginalExamPaperCore == originalExamPaperCore);
            if (originalExamPaper == null)
                throw new Exception($"Không tìm thấy đề thi gốc với mã '{originalExamPaperCore}'");

            var subjectId = originalExamPaper.SubjectId;
            var now = DateTimeHelper.GetVietnamTime();
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo đề hoán vị.");

            // Tạo các đề thi hoán vị
            for (int i = 1; i <= count; i++)
            {
                // Tạo mã duy nhất cho đề thi hoán vị bằng cách sử dụng timestamp và số thứ tự
                var timestamp = now.ToString("yyyyMMddHHmmss");
                var shuffledPaperCore = $"{originalExamPaperCore}_HV_{timestamp}_{i:D3}";
                // Tạo đề thi hoán vị
                var shuffledExamPaper = new ShuffledExamPaper
                {
                    Title = $"{originalExamPaper.Title} - Hoán vị {i}",
                    ShuffledExamPaperCore = shuffledPaperCore,
                    OriginalExamPaperId = originalExamPaper.OriginalExamPaperId,
                    SubjectId = subjectId,
                    IsApproved = true,
                    CreatedBy = userId,
                    CreatedAt = now
                };
                await _shuffledExamPaperRepository.AddAsync(shuffledExamPaper);
                
                // Sử dụng Shufflepaper để tạo thứ tự hoán vị cho câu hỏi
                var shuffledQuestions = await Shufflepaper();
                
                // Tạo ShuffledExamPaperDetail cho từng câu hỏi
                var parentQuestionMap = new Dictionary<int, int>(); // Map từ OriginalExamPaperDetailId đến ShuffledExamPaperDetailId
                
                foreach (var question in shuffledQuestions)
                {
                    var shuffledDetail = new ShuffledExamPaperDetail
                    {
                        ShuffledExamPaperId = shuffledExamPaper.ShuffledExamPaperId,
                        Order = question.Order,
                        OriginalExamPaperDetailId = question.OriginalExamPaperDetailId,
                        ParentQuestionId = null,
                        AnswerOrder = await Stringanswers(question.AnswerShuffleInfo),
                        CreatedBy = userId,
                        CreatedAt = now
                    };
                    
                    // Lưu câu hỏi cha trước để có ID thực
                    await _shuffledExamPaperDetailRepository.AddAsync(shuffledDetail);
                    
                    // Lưu mapping để sử dụng cho câu hỏi con
                    parentQuestionMap[question.OriginalExamPaperDetailId] = shuffledDetail.ShuffledExamPaperDetailId;
                    
                    if (await IsParentQuestionWithChildrenAsync(question.OriginalExamPaperDetailId))
                    {
                        // lấy câu hỏi con của câu hỏi cha
                        var a = await GetChildQuestionsByParentIdAsync(question.OriginalExamPaperDetailId); 
                        //số lượng câu hỏi con được phép hoán vị của câu hỏi cha
                        var counts = await _originalExamPaperDetailRepository.GetQueryable()
                            .Where(d => d.ParentQuestionId == question.OriginalExamPaperDetailId && d.CanShuffleQuestion == true)
                            .CountAsync();
                        if (counts >= 2)
                        {
                            var b = await Shufflepaperchild(question.OriginalExamPaperDetailId);
                            foreach (var item in b)
                            {
                                var detail = new ShuffledExamPaperDetail
                                {
                                    ShuffledExamPaperId = shuffledExamPaper.ShuffledExamPaperId,
                                    Order = item.Order,
                                    OriginalExamPaperDetailId = item.OriginalExamPaperDetailId,
                                    ParentQuestionId = shuffledDetail.ShuffledExamPaperDetailId, // Bây giờ đã có ID thực
                                    AnswerOrder = await Stringanswers(item.AnswerShuffleInfo),
                                    CreatedBy = userId,
                                    CreatedAt = now
                                };
                                await _shuffledExamPaperDetailRepository.AddAsync(detail);
                            }
                        }
                        else
                        {
                            foreach (var child in a)
                            {
                                var detail = new ShuffledExamPaperDetail
                                {
                                    ShuffledExamPaperId = shuffledExamPaper.ShuffledExamPaperId,
                                    Order = child.Order,
                                    OriginalExamPaperDetailId = child.OriginalExamPaperDetailId,
                                    ParentQuestionId = shuffledDetail.ShuffledExamPaperDetailId, // Bây giờ đã có ID thực
                                    AnswerOrder = await Stringanswers(child.AnswerShuffleInfo),
                                    CreatedBy = userId,
                                    CreatedAt = now
                                };
                                await _shuffledExamPaperDetailRepository.AddAsync(detail);
                            }
                        }
                    }
                }
                
                // Tạo AnswerKey cho đề thi hoán vị vừa tạo và lưu vào database
                var answerKey = await GenerateAnswerKeyAsync(shuffledPaperCore);
                
                // Cập nhật AnswerKey cho đề thi hoán vị vừa tạo
                var shuffledExamPaperToUpdate = await _shuffledExamPaperRepository.GetQueryable()
                    .FirstOrDefaultAsync(s => s.ShuffledExamPaperCore == shuffledPaperCore);
                
                if (shuffledExamPaperToUpdate != null)
                {
                    shuffledExamPaperToUpdate.AnswerKey = answerKey;
                    await _shuffledExamPaperRepository.UpdateAsync(shuffledExamPaperToUpdate);
                }
            }
        }
        
        
        
        
        //lấy câu hỏi con của 1 câu hỏi cha
        public async Task<IEnumerable<OriginalExamPaperDetail>> GetChildQuestionsByParentIdAsync(int parentQuestionId)
        {
            var childQuestions = await _originalExamPaperDetailRepository.GetQueryable()
                .Where(d => d.ParentQuestionId == parentQuestionId) 
                .Include(d => d.OriginalExamPaper) 
                .OrderBy(d => d.Order) 
                .ToListAsync();

            return childQuestions;
        }
        

        
        public async Task<IEnumerable<OriginalExamPaperDetail>> Shufflepaperchild(int parentQuestionId)
        {
            // Bước 1: Lấy danh sách câu hỏi có thể hoán vị
            var a = (await GetShuffleableChildQuestionsByParentIdAsync(parentQuestionId)).ToList();
            // Bước 2: Lưu lại danh sách order hiện tại
            var originalOrders = a.Select(q => q.Order).ToList();
            
            // Khởi tạo danh sách chứa kết quả đã hoán vị
            var shuffles = new List<OriginalExamPaperDetail>();
            
            // Khởi tạo bộ sinh số ngẫu nhiên
            var random = new Random();
            
            
            foreach (var sf in a)
            {
                // Chọn ngẫu nhiên 1 order còn lại
                int index = random.Next(originalOrders.Count);
                int randomOrder = originalOrders[index];

                // Tạo bản sao để không thay đổi entity gốc
                var shuffledQuestion = new OriginalExamPaperDetail
                {
                    OriginalExamPaperDetailId = sf.OriginalExamPaperDetailId,
                    Order = randomOrder, // Gán order mới cho bản sao
                    QuestionContent = sf.QuestionContent,
                    Answer1 = sf.Answer1,
                    Answer2 = sf.Answer2,
                    Answer3 = sf.Answer3,
                    Answer4 = sf.Answer4,
                    CorrectAnswerIndex = sf.CorrectAnswerIndex,
                    CanShuffleQuestion = sf.CanShuffleQuestion,
                    AnswerShuffleInfo = sf.AnswerShuffleInfo,
                    ParentQuestionId = sf.ParentQuestionId,
                    ChapterId = sf.ChapterId,
                    OriginalExamPaperId = sf.OriginalExamPaperId
                };

                // Loại bỏ order đã dùng ra khỏi danh sách
                originalOrders.RemoveAt(index);

                // Thêm vào danh sách kết quả
                shuffles.Add(shuffledQuestion);
            }
            var b =  await GetNonShuffleableChildQuestionsByParentIdAsync(parentQuestionId);//chưa sửa 
            var result = shuffles.Concat(b);
            
            // Sắp xếp kết quả theo thứ tự tăng dần của trường Order
            return result.OrderBy(x => x.Order);
            
        }
        public async Task<IEnumerable<OriginalExamPaperDetail>> GetShuffleableChildQuestionsByParentIdAsync(int parentQuestionId)
        {
            var childQuestions = await _originalExamPaperDetailRepository.GetQueryable()
                .Where(d => d.ParentQuestionId == parentQuestionId && d.CanShuffleQuestion == true)
                .ToListAsync();

            return childQuestions;
        }
        
        public async Task<IEnumerable<OriginalExamPaperDetail>> GetNonShuffleableChildQuestionsByParentIdAsync(int parentQuestionId)
        {
            var childQuestions = await _originalExamPaperDetailRepository.GetQueryable()
                .Where(d => d.ParentQuestionId == parentQuestionId && d.CanShuffleQuestion == false)
                .ToListAsync();
            return childQuestions;
        }
        
        public async Task<bool> IsParentQuestionWithChildrenAsync(int questionId)
        {
            var question = await _originalExamPaperDetailRepository.GetQueryable()
                .Where(d => d.OriginalExamPaperDetailId == questionId && 
                            d.ParentQuestionId == null)
                .Include(d => d.ChildQuestions)
                .FirstOrDefaultAsync();

            return question != null && question.ChildQuestions != null && question.ChildQuestions.Any();
        }

        
        
        public async Task<string> Stringanswers(string? keysanswer)
        {
            if (string.IsNullOrWhiteSpace(keysanswer))
                return "";

            var dict = JsonSerializer.Deserialize<Dictionary<string, bool>>(keysanswer);
            if (dict == null || dict.Count == 0)
                return "";

            var allPositions = dict.Keys.Select(k => int.Parse(k)).OrderBy(x => x).ToList();
            var truePositions = allPositions.Where(pos => dict[pos.ToString()]).ToList();

            if (truePositions.Count < 2)
                return string.Join("", allPositions);

            // Tạo bản sao để shuffle
            List<int> shuffled = new List<int>(truePositions);
            var rng = new Random();

            // Shuffle cho đến khi khác với ban đầu
            int maxRetry = 10;
            int retry = 0;
            do
            {
                FisherYatesShuffle(shuffled, rng);
                retry++;
            }
            while (Enumerable.SequenceEqual(shuffled, truePositions) && retry < maxRetry);

            // Nếu vẫn không khác (cực hiếm) thì giữ nguyên
            if (Enumerable.SequenceEqual(shuffled, truePositions))
                return string.Join("", allPositions);

            // Tạo kết quả
            var result = new List<int>();
            foreach (var pos in allPositions)
            {
                if (truePositions.Contains(pos))
                {
                    result.Add(shuffled[0]);
                    shuffled.RemoveAt(0);
                }
                else
                {
                    result.Add(pos);
                }
            }

            return string.Join("", result);
        }

        // Thuật toán hoán vị ngẫu nhiên chuẩn
        private void FisherYatesShuffle<T>(IList<T> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }



























        
        public async Task<IEnumerable<OriginalExamPaperDetail>> GetOriginalExamPaperDetailsByCanShuffleQuestionAsync()
        {
            var details = await _originalExamPaperDetailRepository.GetQueryable()
                .Where(d => d.CanShuffleQuestion == true && d.ParentQuestionId == null)
                .OrderBy(d => d.Order)
                .ToListAsync();

            return details;
        }

        public async Task<IEnumerable<OriginalExamPaperDetail>> GetOriginalExamPaperDetailsByCannotShuffleQuestionAsync()
        {
            var details = await _originalExamPaperDetailRepository.GetQueryable()
                .Where(d => d.CanShuffleQuestion == false && d.ParentQuestionId == null)
                .OrderBy(d => d.Order)
                .ToListAsync();

            return details;
        }
        

        public async Task<IEnumerable<OriginalExamPaperDetail>> Shufflepaper()
        {
            // Bước 1: Lấy danh sách câu hỏi có thể hoán vị
            var shuffleCandidates = (await GetOriginalExamPaperDetailsByCanShuffleQuestionAsync()).ToList();

            // Bước 2: Lưu lại danh sách order hiện tại
            var originalOrders = shuffleCandidates.Select(q => q.Order).ToList();

            // Khởi tạo danh sách chứa kết quả đã hoán vị
            var shuffle = new List<OriginalExamPaperDetail>();

            // Khởi tạo bộ sinh số ngẫu nhiên
            var random = new Random();

            foreach (var sf in shuffleCandidates)
            {
                // Chọn ngẫu nhiên 1 order còn lại
                int index = random.Next(originalOrders.Count);
                int randomOrder = originalOrders[index];

                // Tạo bản sao để không thay đổi entity gốc
                var shuffledQuestion = new OriginalExamPaperDetail
                {
                    OriginalExamPaperDetailId = sf.OriginalExamPaperDetailId,
                    Order = randomOrder, // Gán order mới cho bản sao
                    QuestionContent = sf.QuestionContent,
                    Answer1 = sf.Answer1,
                    Answer2 = sf.Answer2,
                    Answer3 = sf.Answer3,
                    Answer4 = sf.Answer4,
                    CorrectAnswerIndex = sf.CorrectAnswerIndex,
                    CanShuffleQuestion = sf.CanShuffleQuestion,
                    AnswerShuffleInfo = sf.AnswerShuffleInfo,
                    ParentQuestionId = sf.ParentQuestionId,
                    ChapterId = sf.ChapterId,
                    OriginalExamPaperId = sf.OriginalExamPaperId
                };

                // Loại bỏ order đã dùng ra khỏi danh sách
                originalOrders.RemoveAt(index);

                // Thêm vào danh sách kết quả
                shuffle.Add(shuffledQuestion);
            }
            var a =  await GetOriginalExamPaperDetailsByCannotShuffleQuestionAsync();
            var result = shuffle.Concat(a);
            
            // Sắp xếp kết quả theo thứ tự tăng dần của trường Order
            return result.OrderBy(x => x.Order);
        }

        public async Task<string> GenerateAnswerKeyAsync(string shuffledExamPaperCore)
        {
            if (string.IsNullOrWhiteSpace(shuffledExamPaperCore))
                throw new ArgumentException("Mã đề thi hoán vị không hợp lệ");

            // Lấy đề thi hoán vị và các chi tiết
            var shuffledExamPaper = await _shuffledExamPaperRepository.GetQueryable()
                .Include(s => s.ShuffledExamPaperDetails)
                .ThenInclude(d => d.OriginalExamPaperDetail)
                .ThenInclude(od => od.ChildQuestions)
                .FirstOrDefaultAsync(s => s.ShuffledExamPaperCore == shuffledExamPaperCore);

            if (shuffledExamPaper == null)
                throw new Exception($"Không tìm thấy đề thi hoán vị với mã '{shuffledExamPaperCore}'");

            var answerKeyBuilder = new StringBuilder();
            var questionNumber = 1;

            // Lấy tất cả câu hỏi cha (parent questions) và câu hỏi độc lập
            var parentAndIndependentQuestions = shuffledExamPaper.ShuffledExamPaperDetails
                .Where(d => d.ParentQuestionId == null)
                .OrderBy(d => d.Order)
                .ToList();

            foreach (var question in parentAndIndependentQuestions)
            {
                var originalDetail = question.OriginalExamPaperDetail;
                
                // Kiểm tra xem có phải câu hỏi cha có câu hỏi con không
                if (originalDetail.ChildQuestions != null && originalDetail.ChildQuestions.Any())
                {
                    // Đây là câu hỏi nhóm
                    answerKeyBuilder.Append($"({questionNumber},");
                    
                    // Lấy các câu hỏi con của câu hỏi cha này
                    var childQuestions = shuffledExamPaper.ShuffledExamPaperDetails
                        .Where(d => d.ParentQuestionId == question.ShuffledExamPaperDetailId)
                        .OrderBy(d => d.Order)
                        .ToList();

                    var childAnswerKeyBuilder = new StringBuilder();
                    var childNumber = 1;

                    foreach (var childQuestion in childQuestions)
                    {
                        var childOriginalDetail = childQuestion.OriginalExamPaperDetail;
                        var correctAnswer = GetCorrectAnswerWithShuffle(childOriginalDetail, childQuestion.AnswerOrder);
                        
                        if (childNumber > 1)
                            childAnswerKeyBuilder.Append(";");
                        childAnswerKeyBuilder.Append($"({childNumber},{correctAnswer})");
                        childNumber++;
                    }

                    answerKeyBuilder.Append(childAnswerKeyBuilder.ToString());
                    answerKeyBuilder.Append(")");
                }
                else
                {
                    // Đây là câu hỏi đơn
                    var correctAnswer = GetCorrectAnswerWithShuffle(originalDetail, question.AnswerOrder);
                    answerKeyBuilder.Append($"({questionNumber},{correctAnswer})");
                }

                // Thêm dấu chấm phẩy nếu không phải câu cuối cùng
                if (questionNumber < parentAndIndependentQuestions.Count)
                {
                    answerKeyBuilder.Append(";");
                }

                questionNumber++;
            }

            return answerKeyBuilder.ToString();
        }

        private string GetCorrectAnswerWithShuffle(OriginalExamPaperDetail originalDetail, string answerOrder)
        {
            if (originalDetail.CorrectAnswerIndex == null)
                return "";

            // Nếu không có thông tin hoán vị đáp án, trả về đáp án gốc
            if (string.IsNullOrWhiteSpace(answerOrder))
            {
                return GetAnswerLetter(originalDetail.CorrectAnswerIndex.Value);
            }

            // Nếu có thông tin hoán vị, tìm đáp án đúng sau khi hoán vị
            var correctPosition = originalDetail.CorrectAnswerIndex.Value;
            if (correctPosition <= answerOrder.Length)
            {
                // Lấy ký tự ở vị trí CorrectAnswerIndex trong AnswerOrder
                // Ví dụ: AnswerOrder = "4312", CorrectAnswerIndex = 2
                // Thì lấy ký tự thứ 2 trong "4312" = "3"
                var shuffledAnswerIndex = int.Parse(answerOrder[correctPosition - 1].ToString());
                return GetAnswerLetter(shuffledAnswerIndex);
            }

            return GetAnswerLetter(originalDetail.CorrectAnswerIndex.Value);
        }

        private string GetAnswerLetter(int answerIndex)
        {
            return answerIndex switch
            {
                1 => "A",
                2 => "B", 
                3 => "C",
                4 => "D",
                _ => ""
            };
        }

    }
}