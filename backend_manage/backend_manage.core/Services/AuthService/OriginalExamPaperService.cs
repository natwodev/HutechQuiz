using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Xml.Serialization;
using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services;
using backend_manage.core.Services.Interfaces;
using backend_manage.shared.DTOs;
using backend_manage.shared.DTOs.EPZ;
using ICSharpCode.SharpZipLib.Zip;
using System.IO.Compression;
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
        private readonly ExamZipImportService _examZipImportService;

        public OriginalExamPaperService(
            IRepository<Subject> subjectRepository,
            IRepository<OriginalExamPaper> originalExamPaperRepository,
            IRepository<Chapter> chapterRepository,
            IRepository<OriginalExamPaperDetail> originalExamPaperDetailRepository,
            IRepository<Answers> answersRepository,
            IHttpContextAccessor httpContextAccessor,
            IRepository<ShuffledExamPaper> shuffledExamPaperRepository,
            IRepository<ExamSessionSubject> examSessionSubjectRepository,
            IMapper mapper,
            ExamZipImportService examZipImportService)
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
            _examZipImportService = examZipImportService;
        }

        public async Task<OriginalExamPaperDto> CreateAsync(CreateOriginalExamPaperRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.OriginalExamPaperCore))
                throw new ArgumentException("Mã đề thi gốc không được để trống");
            
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Tiêu đề đề thi không được để trống");

            // Kiểm tra xem OriginalExamPaperCore đã tồn tại chưa
            var existingExamPaper = await _originalExamPaperRepository.GetQueryable()
                .AnyAsync(x => x.OriginalExamPaperCore == request.OriginalExamPaperCore);
            if (existingExamPaper)
                throw new Exception($"Đã tồn tại đề thi với mã '{request.OriginalExamPaperCore}' trong hệ thống.");

            // Kiểm tra SubjectId có tồn tại không
            var subjectExists = await _subjectRepository.GetQueryable()
                .AnyAsync(s => s.SubjectId == request.SubjectId);
            if (!subjectExists)
                throw new Exception($"Không tìm thấy môn học với ID '{request.SubjectId}'.");

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo đề thi gốc.");

            var now = DateTimeHelper.GetVietnamTime();

            // Tạo OriginalExamPaper
            var originalExamPaper = new OriginalExamPaper
            {
                OriginalExamPaperCore = request.OriginalExamPaperCore,
                Title = request.Title,
                Description = request.Description,
                SubjectId = request.SubjectId,
                AllowViewMaterials = request.AllowViewMaterials,
                DurationMinutes = request.DurationMinutes,
                IsApproved = request.IsApproved ?? false, // Mặc định chưa phê duyệt
                IsManualCreated = true, // Đề thi được tạo thủ công
                TotalQuestions = 0, // Sẽ cập nhật khi thêm câu hỏi
                TotalShuffledPapers = 0,
                KeyValueList = null,
                CreatedBy = userId,
                CreatedAt = now
            };

            await _originalExamPaperRepository.AddAsync(originalExamPaper);

            // Map sang DTO để trả về
            var dto = _mapper.Map<OriginalExamPaperDto>(originalExamPaper);
            dto.Details = new List<OriginalExamPaperDetailDto>(); // Chưa có câu hỏi nào

            return dto;
        }

        public async Task<OriginalExamPaperDto> UpdateAsync(UpdateOriginalExamPaperRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.OriginalExamPaperCore))
                throw new ArgumentException("Mã đề thi gốc không được để trống");
            
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Tiêu đề đề thi không được để trống");

            // Kiểm tra xem OriginalExamPaperCore có tồn tại không
            var existingExamPaper = await _originalExamPaperRepository.GetQueryable()
                .FirstOrDefaultAsync(x => x.OriginalExamPaperCore == request.OriginalExamPaperCore);
            if (existingExamPaper == null)
                throw new Exception($"Không tìm thấy đề thi với mã '{request.OriginalExamPaperCore}' trong hệ thống.");

            // Kiểm tra SubjectId có tồn tại không
            var subjectExists = await _subjectRepository.GetQueryable()
                .AnyAsync(s => s.SubjectId == request.SubjectId);
            if (!subjectExists)
                throw new Exception($"Không tìm thấy môn học với ID '{request.SubjectId}'.");

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật đề thi gốc.");

            var now = DateTimeHelper.GetVietnamTime();

            // Cập nhật OriginalExamPaper
            existingExamPaper.Title = request.Title;
            existingExamPaper.Description = request.Description;
            existingExamPaper.SubjectId = request.SubjectId;
            existingExamPaper.AllowViewMaterials = request.AllowViewMaterials;
            existingExamPaper.DurationMinutes = request.DurationMinutes;
            existingExamPaper.IsApproved = request.IsApproved ?? existingExamPaper.IsApproved;
            existingExamPaper.UpdatedBy = userId;
            existingExamPaper.UpdatedAt = now;

            await _originalExamPaperRepository.UpdateAsync(existingExamPaper);

            // Map sang DTO để trả về
            var dto = _mapper.Map<OriginalExamPaperDto>(existingExamPaper);
            
            // Load Details nếu cần
            var examPaperWithDetails = await _originalExamPaperRepository.GetQueryable()
                .Where(x => x.OriginalExamPaperId == existingExamPaper.OriginalExamPaperId)
                .Include(x => x.OriginalExamPaperDetails)
                    .ThenInclude(d => d.Answers)
                .Include(x => x.OriginalExamPaperDetails)
                    .ThenInclude(d => d.ChildQuestions)
                        .ThenInclude(c => c.Answers)
                .FirstOrDefaultAsync();
            
            if (examPaperWithDetails != null)
            {
                dto.Details = _mapper.Map<List<OriginalExamPaperDetailDto>>(examPaperWithDetails.OriginalExamPaperDetails);
            }
            else
            {
                dto.Details = new List<OriginalExamPaperDetailDto>();
            }

            return dto;
        }

        #region ExtractAndReadXmlAsync thêm đề thi
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
                OriginalExamPaperCore = originalExamPaperCore,
                IsApproved = true, // Đề thi gốc mặc định được duyệt khi thêm mới
                IsManualCreated = false, // Đề thi được import từ file XML
                TotalShuffledPapers = 0,
                // Mặc định khi import là đề đóng (không được phép xem tài liệu)
                AllowViewMaterials = false
            };
            await _originalExamPaperRepository.AddAsync(originalExamPaper);

            // Dictionary để lưu trữ mapping giữa câu hỏi và đáp án đúng
            var questionAnswerMapping = new Dictionary<int, int>();

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
                                    int? correctAnswerId = null;
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
                                        
                                        // Lưu ID của đáp án đúng
                                        if (answer.LaDapAn)
                                        {
                                            correctAnswerId = answerEntity.AnswerId;
                                        }
                                    }
                                    
                                    // Lưu mapping giữa câu hỏi và đáp án đúng
                                    if (correctAnswerId.HasValue)
                                    {
                                        questionAnswerMapping[detail.OriginalExamPaperDetailId] = correctAnswerId.Value;
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
                                int? correctAnswerId = null;
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
                                    
                                    // Lưu ID của đáp án đúng
                                    if (answer.LaDapAn)
                                    {
                                        correctAnswerId = answerEntity.AnswerId;
                                    }
                                }
                                
                                // Lưu mapping giữa câu hỏi và đáp án đúng
                                if (correctAnswerId.HasValue)
                                {
                                    questionAnswerMapping[detail.OriginalExamPaperDetailId] = correctAnswerId.Value;
                                }
                            }
                        }
                    }
                }
            }
            
            // Tạo KeyValueList từ mapping giữa câu hỏi và đáp án đúng
            if (questionAnswerMapping.Any())
            {
                var keyValuePairs = questionAnswerMapping
                    .Select(kvp => $"({kvp.Key}:{kvp.Value})")
                    .ToList();
                var keyValueList = string.Join(";", keyValuePairs) + ";";
                
                // Cập nhật OriginalExamPaper với KeyValueList
                originalExamPaper.KeyValueList = keyValueList;
                await _originalExamPaperRepository.UpdateAsync(originalExamPaper);
            }
        }
        

        #endregion

        #region Import từ Word (.docx) với format CLO

        public async Task ImportFromWordAsync(IFormFile file, string originalExamPaperCore, int subjectId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ hoặc rỗng");

            // Kiểm tra file là .docx hay .zip
            bool isZip = file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
            bool isDocx = file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);

            if (!isZip && !isDocx)
                throw new ArgumentException("File phải có đuôi .docx hoặc .zip");

            var exists = await _originalExamPaperRepository.GetQueryable()
                .AnyAsync(x => x.OriginalExamPaperCore == originalExamPaperCore);
            if (exists)
                throw new Exception($"Đã tồn tại đề thi với mã '{originalExamPaperCore}' trong hệ thống.");

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo đề thi gốc.");

            var now = DateTimeHelper.GetVietnamTime();

            var subject = await _subjectRepository.GetQueryable()
                .FirstOrDefaultAsync(s => s.SubjectId == subjectId);
            if (subject == null)
            {
                throw new Exception($"Không tìm thấy môn học với ID '{subjectId}'.");
            }

            Stream wordStream;

            // Nếu là ZIP, extract file Word từ trong đó
            if (isZip)
            {
                var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                try
                {
                    // Extract ZIP file - sử dụng System.IO.Compression cho phần mới
                    using (var zipStream = file.OpenReadStream())
                    using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read))
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
                    }

                    // Đọc file Word vào MemoryStream
                    wordStream = new MemoryStream();
                    using (var fileStream = File.OpenRead(examDocxPath))
                    {
                        await fileStream.CopyToAsync(wordStream);
                    }
                    wordStream.Position = 0;
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
                        catch
                        {
                            // Ignore cleanup errors
                        }
                    }
                }
            }
            else
            {
                // Nếu là .docx trực tiếp, đọc vào stream
                wordStream = new MemoryStream();
                await file.CopyToAsync(wordStream);
                wordStream.Position = 0;
            }

            // Parse Word document với format CLO
            using (wordStream)
            {
                var parsed = WordParserService.Parse(wordStream);
                if (parsed.Count == 0)
                    throw new Exception("Không tìm thấy câu hỏi nào theo format CLO trong file Word.");

                var originalExamPaper = new OriginalExamPaper
                {
                    Title = Path.GetFileNameWithoutExtension(file.FileName),
                    Description = null,
                    SubjectId = subject.SubjectId,
                    CreatedBy = userId,
                    CreatedAt = now,
                    DurationMinutes = 0,
                    TotalQuestions = parsed.Count,
                    OriginalExamPaperCore = originalExamPaperCore,
                    IsApproved = true,
                    IsManualCreated = false,
                    TotalShuffledPapers = 0,
                    AllowViewMaterials = false
                };

                await _originalExamPaperRepository.AddAsync(originalExamPaper);

                var details = new List<OriginalExamPaperDetail>();
                var answers = new List<Answers>();

                int qOrder = 1;

                foreach (var q in parsed)
                {
                    var detail = new OriginalExamPaperDetail
                    {
                        OriginalExamPaperId = originalExamPaper.OriginalExamPaperId,
                        Order = qOrder++,
                        QuestionContent = q.Stem,
                        CorrectAnswerIndex = q.CorrectAnswerLabel != null
                            ? q.Answers.FindIndex(a => a.Label == q.CorrectAnswerLabel) + 1
                            : null,
                        CreatedAt = now,
                        CreatedBy = userId
                    };

                    details.Add(detail);

                    int aOrder = 1;
                    foreach (var a in q.Answers)
                    {
                        answers.Add(new Answers
                        {
                            OriginalExamPaperDetail = detail,
                            Order = aOrder++,
                            AnswerContent = a.Content,
                            IsCorrect = q.CorrectAnswerLabel == a.Label,
                            CreatedAt = now,
                            CreatedBy = userId
                        });
                    }
                }

                foreach (var detail in details)
                {
                    await _originalExamPaperDetailRepository.AddAsync(detail);
                }
                foreach (var answer in answers)
                {
                    await _answersRepository.AddAsync(answer);
                }
            }
        }

        public async Task ImportFromZipAsync(IFormFile file, string originalExamPaperCore, int subjectId)
        {
            await _examZipImportService.ImportFromZipAsync(file, originalExamPaperCore, subjectId);
        }

        #endregion
       
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

        public async Task<List<OriginalExamDto>> GetAllAsync()
        {
            var examPapers = await _originalExamPaperRepository.GetQueryable()
                .Include(x => x.Subject)
                .Include(x => x.ShuffledExamPapers.Where(s => !s.IsDeleted))
                .Where(x => !x.IsDeleted)
                .ToListAsync();
            
            var result = examPapers.Select(x =>
            {
                var dto = _mapper.Map<OriginalExamDto>(x);
                // Đếm số lượng đề hoán vị không bị xóa
                dto.TotalShuffledPapers = x.ShuffledExamPapers?.Count(s => !s.IsDeleted) ?? 0;
                return dto;
            }).ToList();
            
            return result;
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
                   // AnswerKey = originalExamPaper.KeyValueList,
                    SubjectId = subjectId,
                    IsApproved = true,
                    // Kế thừa cấu hình đề mở/đề đóng từ đề gốc
                    AllowViewMaterials = originalExamPaper.AllowViewMaterials,
                    CreatedBy = userId,
                    CreatedAt = now
                };
                await _shuffledExamPaperRepository.AddAsync(shuffledExamPaper);
                
                var result = new List<OriginalExamPaperDetailShufferDto>();
                var resulta = new List<AnswerShufferDto>();
                var qu = await ShuffleParentQuestionsAsync(originalExamPaper.OriginalExamPaperId);
                
                foreach (var q in qu)
                {
                    var sfc = await ShuffleChildQuestionsAsync(q.OriginalExamPaperDetailId);
                    result.AddRange(sfc);
                   
                }
                
                result.AddRange(qu);
                
                // Sắp xếp result theo trường Order
                result = result.OrderBy(q => q.Order).ToList();
                foreach (var q in result)
                {
                    var sfa = await ShuffleAnswersAsync(q.OriginalExamPaperDetailId);
                    resulta.AddRange(sfa);
                }
                
                // Sắp xếp resulta theo trường Order
                resulta = resulta.OrderBy(a => a.Order).ToList();
                
                // Map vào QuestionStructureDto
                var questionStructure = MapToQuestionStructure(result, resulta);

         

                // Lưu cấu trúc câu hỏi vào ShuffledExamPaper
                shuffledExamPaper.QuestionStructure = JsonConvert.SerializeObject(questionStructure);

                // In ra console để kiểm tra
                var emptyTemplate = BuildEmptyAnswerTemplateFromQuestionStructure(questionStructure);
                Console.WriteLine($"[CreateShuffledExamPapers] {shuffledPaperCore} -> EmptyAnswerTemplate: {emptyTemplate}");

                // Căn theo template (thứ tự hiển thị) và lấy value từ OriginalExamPaper.KeyValueList (thứ tự có thể khác)
                var mergedAnswerKey = MergeAnswerKeyFromTemplate(emptyTemplate, originalExamPaper.KeyValueList);
                Console.WriteLine($"[CreateShuffledExamPapers] {shuffledPaperCore} -> MergedAnswerKey    : {mergedAnswerKey}");

                shuffledExamPaper.AnswerKey = mergedAnswerKey;
                // Cập nhật ShuffledExamPaper với cấu trúc câu hỏi
                await _shuffledExamPaperRepository.UpdateAsync(shuffledExamPaper);
                
            }
        }
        
         
         
         
         
         
         
         
         
        //hoán vị câu hỏi cha và câu hỏi đơn (khong chỉnh sửa)
        public async Task<List<OriginalExamPaperDetailShufferDto>> ShuffleParentQuestionsAsync(int originalExamPaperId)
        {
            var (parentShufflable, parentNonShufflable) = await GetParentQuestionsAsync(originalExamPaperId);
            
            var result = new List<OriginalExamPaperDetailShufferDto>();
            
            // Nếu số câu hỏi có thể hoán vị < 2 thì gộp danh sách lại
            if (parentShufflable.Count() < 2)
            {
                // Gộp cả hai danh sách và sắp xếp theo thứ tự gốc
                result.AddRange(MapToShufferDto(parentShufflable));
                result.AddRange(MapToShufferDto(parentNonShufflable));
                result = result.OrderBy(q => q.Order).ToList();
            }
            else
            {
                var random = new Random();
                List<int> orders = new List<int>();
                // Thêm order của parentShufflable vào danh sách orders
                orders.AddRange(parentShufflable.Select(q => q.Order));
                var rs = MapToShufferDto(parentShufflable);
                foreach (var r in rs)
                {
                    // Lấy index ngẫu nhiên trong list orders
                    int randomIndex = random.Next(orders.Count);

                    // Lấy giá trị Order ở vị trí đó
                    int selectedOrder = orders[randomIndex];

                    // Gán vào DTO
                    r.Order = selectedOrder;

                    // Xóa phần tử đã dùng để tránh trùng
                    orders.RemoveAt(randomIndex);
                    result.Add(r);
                }
                result.AddRange(MapToShufferDto(parentNonShufflable));
            }
            
            return result.OrderBy(q => q.Order).ToList();;
        }
         
        
        
        public async Task<List<OriginalExamPaperDetailShufferDto>> ShuffleChildQuestionsAsync(int originalExamPaperDetailId)
        {
            var (childShufflable, childNonShufflable) = await GetChildQuestionsAsync(originalExamPaperDetailId);
            
            var result = new List<OriginalExamPaperDetailShufferDto>();
            
            // Nếu số câu hỏi con có thể hoán vị < 2 thì gộp danh sách lại
            if (childShufflable.Count() < 2)
            {
                // Gộp cả hai danh sách và sắp xếp theo thứ tự gốc
                result.AddRange(MapToShufferDto(childShufflable));
                result.AddRange(MapToShufferDto(childNonShufflable));
            }
            else
            {
                var random = new Random();
                List<int> orders = new List<int>();
                // Thêm order của childShufflable vào danh sách orders
                orders.AddRange(childShufflable.Select(q => q.Order));
                var rs = MapToShufferDto(childShufflable);
                foreach (var r in rs)
                {
                    // Lấy index ngẫu nhiên trong list orders
                    int randomIndex = random.Next(orders.Count);

                    // Lấy giá trị Order ở vị trí đó
                    int selectedOrder = orders[randomIndex];

                    // Gán vào DTO
                    r.Order = selectedOrder;

                    // Xóa phần tử đã dùng để tránh trùng
                    orders.RemoveAt(randomIndex);
                    result.Add(r);
                }
                result.AddRange(MapToShufferDto(childNonShufflable));
            }
            
            return result.OrderBy(q => q.Order).ToList();
        }

        
        public async Task<IEnumerable<AnswerShufferDto>> ShuffleAnswersAsync(int originalExamPaperDetailId)
        {
            var result = new List<AnswerShufferDto>();

            var (shufflableAnswers, nonShufflableAnswers) = await GetAnswersByOriginalExamPaperDetailIdAsync(originalExamPaperDetailId);
           
            var random = new Random();
            
            // Nếu số câu trả lời có thể hoán vị < 2 thì gộp danh sách lại
            if (shufflableAnswers.Count() < 2)
            {
                // Gộp cả hai danh sách và sắp xếp theo thứ tự gốc
                result.AddRange(MapToShufferAnswersDto(shufflableAnswers));
                result.AddRange(MapToShufferAnswersDto(nonShufflableAnswers));
            }
            else
            {
                // Tạo danh sách thứ tự để hoán vị
                List<int> orders = new List<int>();
                orders.AddRange(shufflableAnswers.Select(a => a.Order));
                var rs = MapToShufferAnswersDto(shufflableAnswers);
                // Hoán vị các câu trả lời có thể hoán vị
                foreach (var answer in rs)
                {
                    // Lấy index ngẫu nhiên trong list orders
                    int randomIndex = random.Next(orders.Count);
                    // Lấy giá trị Order ở vị trí đó
                    int selectedOrder = orders[randomIndex];
                    // Gán vào DTO
                    answer.Order = selectedOrder;
                    // Xóa phần tử đã dùng để tránh trùng
                    orders.RemoveAt(randomIndex);
                    result.Add(answer);
                }
                
                // Thêm các câu trả lời không thể hoán vị với thứ tự gốc
                result.AddRange(MapToShufferAnswersDto(nonShufflableAnswers));
            }
            
            return result.OrderBy(a => a.Order).ToList();
        }

        
        
        // Helper method để map từ OriginalExamPaperDetail sang OriginalExamPaperDetailShufferDto
        private List<OriginalExamPaperDetailShufferDto> MapToShufferDto(IEnumerable<OriginalExamPaperDetail> details)
        {
            return details.Select(detail => new OriginalExamPaperDetailShufferDto
            {
                OriginalExamPaperDetailId = detail.OriginalExamPaperDetailId,
                Order = detail.Order,
                QuestionContent = detail.QuestionContent,
                CorrectAnswerIndex = detail.CorrectAnswerIndex,
                ParentQuestionId = detail.ParentQuestionId,
                CanShuffleQuestion = detail.CanShuffleQuestion,
            }).ToList();
        }
         
        
        private List<AnswerShufferDto> MapToShufferAnswersDto(IEnumerable<Answers> answers)
        {
            return answers.Select(answer => new AnswerShufferDto
            {
                AnswerId = answer.AnswerId,
                Order = answer.Order,
                AnswerContent = answer.AnswerContent,
                IsCorrect = answer.IsCorrect,
                CanShuffleAnswer = answer.CanShuffleAnswer,
                OriginalExamPaperDetailId = answer.OriginalExamPaperDetailId
            }).ToList();
        }
        
        
        
        // Phương thức lấy câu hỏi cha theo khả năng hoán vị,danh danh sách câu hỏi không hoán vị
        public async Task<(IEnumerable<OriginalExamPaperDetail> ShufflableQuestions, IEnumerable<OriginalExamPaperDetail> NonShufflableQuestions)> GetParentQuestionsAsync(int originalExamPaperId)
        {
            var parentQuestions = await _originalExamPaperDetailRepository.GetQueryable()
                .Where(q => q.OriginalExamPaperId == originalExamPaperId && q.ParentQuestionId == null)
                .Include(q => q.ParentQuestion)
                .OrderBy(q => q.Order)
                .ToListAsync();

            // Phân loại câu hỏi theo khả năng hoán vị
            var shufflableQuestions = parentQuestions.Where(q => q.CanShuffleQuestion).ToList();
            var nonShufflableQuestions = parentQuestions.Where(q => !q.CanShuffleQuestion).ToList();

            return (shufflableQuestions, nonShufflableQuestions);
        }

        // Phương thức lấy danh sách Answers theo originalExamPaperDetailId với phân loại khả năng hoán vị
        public async Task<(IEnumerable<Answers> ShufflableAnswers, IEnumerable<Answers> NonShufflableAnswers)> GetAnswersByOriginalExamPaperDetailIdAsync(int originalExamPaperDetailId)
        {
            var answers = await _answersRepository.GetQueryable()
                .Where(a => a.OriginalExamPaperDetailId == originalExamPaperDetailId)
                .Include(a => a.OriginalExamPaperDetail)
                .OrderBy(a => a.Order)
                .ToListAsync();

            // Phân loại câu trả lời theo khả năng hoán vị của từng câu trả lời
            var shufflableAnswers = answers.Where(a => a.CanShuffleAnswer == true).ToList();
            var nonShufflableAnswers = answers.Where(a => a.CanShuffleAnswer == false).ToList();

            return (shufflableAnswers, nonShufflableAnswers);
        }


        
        // Phương thức lấy câu hỏi con theo khả năng hoán vị
        public async Task<(IEnumerable<OriginalExamPaperDetail> ShufflableQuestions, IEnumerable<OriginalExamPaperDetail> NonShufflableQuestions)> GetChildQuestionsAsync(int originalExamPaperDetailId)
        {
            var childQuestions = await _originalExamPaperDetailRepository.GetQueryable()
                .Where(q => q.ParentQuestionId == originalExamPaperDetailId)
                .OrderBy(q => q.Order)
                .ToListAsync();

            // Phân loại câu hỏi con theo khả năng hoán vị
            var shufflableQuestions = childQuestions.Where(q => q.CanShuffleQuestion).ToList();
            var nonShufflableQuestions = childQuestions.Where(q => !q.CanShuffleQuestion).ToList();

            return (shufflableQuestions, nonShufflableQuestions);
        }
        
        
        
                // Phương thức lấy danh sách Answers theo OriginalExamPaperId
        public async Task<IEnumerable<Answers>> GetAnswersByOriginalExamPaperIdAsync(int originalExamPaperId)
        {
            var answers = await _answersRepository.GetQueryable()
                .Where(a => a.OriginalExamPaperDetail.OriginalExamPaperId == originalExamPaperId)
                .Include(a => a.OriginalExamPaperDetail)
                .OrderBy(a => a.OriginalExamPaperDetail.Order)
                .ThenBy(a => a.Order)
                .ToListAsync();

            return answers;
        }

        // Phương thức mapping chính để tạo QuestionStructureDto
        private List<QuestionStructureDto> MapToQuestionStructure(List<OriginalExamPaperDetailShufferDto> questions, List<AnswerShufferDto> answers)
        {
            var result = new List<QuestionStructureDto>();
            
            // Lấy tất cả câu hỏi cha (ParentQuestionId = null)
            var parentQuestions = questions
                .Where(q => q.ParentQuestionId == null)
                .OrderBy(q => q.Order)
                .ToList();
            
            foreach (var parentQuestion in parentQuestions)
            {
                var questionStructure = new QuestionStructureDto
                {
                    OriginalExamPaperDetailId = parentQuestion.OriginalExamPaperDetailId,
                    ParentQuestionId = parentQuestion.ParentQuestionId,
                    Order = parentQuestion.Order,
                    ChildQuestions = new List<QuestionStructureDto>(),
                    Answers = new List<AnswerStructureDto>()
                };
                
                // Lấy câu hỏi con của câu hỏi cha này
                var childQuestions = questions
                    .Where(q => q.ParentQuestionId == parentQuestion.OriginalExamPaperDetailId)
                    .OrderBy(q => q.Order)
                    .ToList();
                
                if (childQuestions.Any())
                {
                    // Nếu có câu hỏi con, map câu hỏi con
                    foreach (var childQuestion in childQuestions)
                    {
                        var childStructure = new QuestionStructureDto
                        {
                            OriginalExamPaperDetailId = childQuestion.OriginalExamPaperDetailId,
                            ParentQuestionId = childQuestion.ParentQuestionId,
                            Order = childQuestion.Order,
                            ChildQuestions = new List<QuestionStructureDto>(),
                            Answers = GetAnswersForQuestion(childQuestion.OriginalExamPaperDetailId, answers)
                        };
                        
                        questionStructure.ChildQuestions.Add(childStructure);
                    }
                }
                else
                {
                    // Nếu không có câu hỏi con, lấy câu trả lời trực tiếp
                    questionStructure.Answers = GetAnswersForQuestion(parentQuestion.OriginalExamPaperDetailId, answers);
                }
                
                result.Add(questionStructure);
            }
            
            return result;
        }
        
        // Helper method để lấy câu trả lời cho một câu hỏi
        private List<AnswerStructureDto> GetAnswersForQuestion(int questionId, List<AnswerShufferDto> allAnswers)
        {
            return allAnswers
                .Where(a => a.OriginalExamPaperDetailId == questionId)
                .OrderBy(a => a.Order)
                .Select(answer => new AnswerStructureDto
                {
                    AnswerId = answer.AnswerId,
                    Order = answer.Order,
                    OriginalExamPaperDetailId = answer.OriginalExamPaperDetailId
                })
                .ToList();
        }

        // Tạo template Answer rỗng theo QuestionStructure: (QuestionDetailId:-);
        public string BuildEmptyAnswerTemplateFromQuestionStructure(List<QuestionStructureDto> questionStructure)
        {
            if (questionStructure == null || questionStructure.Count == 0)
            {
                return string.Empty;
            }

            var orderedLeafQuestionIds = new List<int>();

            void Traverse(IEnumerable<QuestionStructureDto> nodes)
            {
                foreach (var node in nodes.OrderBy(n => n.Order))
                {
                    if (node.ChildQuestions != null && node.ChildQuestions.Count > 0)
                    {
                        Traverse(node.ChildQuestions);
                    }
                    else
                    {
                        // Leaf question (có Answer trực tiếp)
                        orderedLeafQuestionIds.Add(node.OriginalExamPaperDetailId);
                    }
                }
            }

            Traverse(questionStructure);

            if (orderedLeafQuestionIds.Count == 0)
            {
                return string.Empty;
            }

            var parts = orderedLeafQuestionIds
                .Select(qid => $"({qid}:-)")
                .ToList();

            return string.Join(";", parts) + ";";
        }

        // Ghép value từ original KeyValueList vào template theo key, bỏ qua thứ tự
        private string MergeAnswerKeyFromTemplate(string emptyTemplate, string? originalKeyValueList)
        {
            if (string.IsNullOrWhiteSpace(emptyTemplate)) return string.Empty;

            // Parse template -> danh sách key theo thứ tự hiển thị
            var templatePairs = emptyTemplate
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.StartsWith("(") && s.EndsWith(")"))
                .Select(s => s.Trim('(', ')'))
                .Select(s => s.Split(':'))
                .Select(parts => new { Key = int.Parse(parts[0]), Value = (string?)null })
                .ToList();

            if (string.IsNullOrWhiteSpace(originalKeyValueList))
            {
                // Không có nguồn value, giữ template dạng (key:-);
                return emptyTemplate;
            }

            // Parse original -> map key -> value
            var originalMap = originalKeyValueList
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.StartsWith("(") && s.EndsWith(")"))
                .Select(s => s.Trim('(', ')'))
                .Select(s => s.Split(':'))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => int.Parse(parts[0]), parts => parts[1]);

            // Build theo thứ tự template nhưng lấy value theo key từ original
            var merged = templatePairs
                .Select(t =>
                {
                    var has = originalMap.TryGetValue(t.Key, out var val);
                    return has ? $"({t.Key}:{val})" : $"({t.Key}:-)";
                })
                .ToList();

            return string.Join(";", merged) + ";";
        }

        public async Task<bool> UpdateAllowViewMaterialsAsync(string originalExamPaperCore, bool allowViewMaterials)
        {
            if (string.IsNullOrWhiteSpace(originalExamPaperCore))
                throw new ArgumentException("Mã đề thi gốc không hợp lệ");

            var originalExamPaper = await _originalExamPaperRepository.GetQueryable()
                .Include(o => o.ShuffledExamPapers)
                .FirstOrDefaultAsync(o => o.OriginalExamPaperCore == originalExamPaperCore);
            
            if (originalExamPaper == null)
                throw new Exception($"Không tìm thấy đề thi gốc với mã '{originalExamPaperCore}'");

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật đề thi.");

            var now = DateTimeHelper.GetVietnamTime();

            // Cập nhật AllowViewMaterials cho đề thi gốc
            originalExamPaper.AllowViewMaterials = allowViewMaterials;
            originalExamPaper.UpdatedBy = userId;
            originalExamPaper.UpdatedAt = now;
            await _originalExamPaperRepository.UpdateAsync(originalExamPaper);

            // Cập nhật AllowViewMaterials cho tất cả đề hoán vị liên quan (chưa bị xóa)
            if (originalExamPaper.ShuffledExamPapers != null && originalExamPaper.ShuffledExamPapers.Any())
            {
                var shuffledPapers = originalExamPaper.ShuffledExamPapers
                    .Where(s => !s.IsDeleted)
                    .ToList();

                foreach (var shuffledPaper in shuffledPapers)
                {
                    shuffledPaper.AllowViewMaterials = allowViewMaterials;
                    shuffledPaper.UpdatedBy = userId;
                    shuffledPaper.UpdatedAt = now;
                    await _shuffledExamPaperRepository.UpdateAsync(shuffledPaper);
                }
            }

            return true;
        }

        public async Task<OriginalExamPaperDetailDto> AddQuestionWithAnswersAsync(CreateQuestionWithAnswersRequest request)
        {
            // Kiểm tra OriginalExamPaper có tồn tại và là đề thi thủ công
            var originalExamPaper = await _originalExamPaperRepository.GetQueryable()
                .FirstOrDefaultAsync(o => o.OriginalExamPaperId == request.OriginalExamPaperId);
            
            if (originalExamPaper == null)
                throw new Exception($"Không tìm thấy đề thi với ID '{request.OriginalExamPaperId}'");
            
            if (!originalExamPaper.IsManualCreated)
                throw new Exception("Chỉ có thể thêm câu hỏi cho đề thi được tạo thủ công");
            
            // Kiểm tra ChapterId có tồn tại (nếu có)
            if (request.ChapterId.HasValue)
            {
                var chapter = await _chapterRepository.GetQueryable()
                    .FirstOrDefaultAsync(c => c.ChapterId == request.ChapterId.Value);
                
                if (chapter == null)
                    throw new Exception($"Không tìm thấy chương với ID '{request.ChapterId.Value}'");
                
                // Kiểm tra Chapter thuộc cùng Subject với OriginalExamPaper
                if (chapter.SubjectId != originalExamPaper.SubjectId)
                    throw new Exception("Chương không thuộc cùng môn học với đề thi");
            }
            
            // Kiểm tra ParentQuestionId nếu có
            if (request.ParentQuestionId.HasValue)
            {
                var parentQuestion = await _originalExamPaperDetailRepository.GetQueryable()
                    .FirstOrDefaultAsync(q => q.OriginalExamPaperDetailId == request.ParentQuestionId.Value);
                
                if (parentQuestion == null)
                    throw new Exception($"Không tìm thấy câu hỏi cha với ID '{request.ParentQuestionId.Value}'");
                
                if (parentQuestion.OriginalExamPaperId != request.OriginalExamPaperId)
                    throw new Exception("Câu hỏi cha không thuộc cùng đề thi");
            }
            
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo câu hỏi.");
            
            var now = DateTimeHelper.GetVietnamTime();
            
            // Tạo OriginalExamPaperDetail
            var questionDetail = new OriginalExamPaperDetail
            {
                OriginalExamPaperId = request.OriginalExamPaperId,
                Order = request.Order,
                QuestionContent = request.QuestionContent,
                CorrectAnswerIndex = request.CorrectAnswerIndex,
                ParentQuestionId = request.ParentQuestionId,
                ChapterId = request.ChapterId,
                CanShuffleQuestion = request.CanShuffleQuestion,
                CreatedBy = userId,
                CreatedAt = now
            };
            
            await _originalExamPaperDetailRepository.AddAsync(questionDetail);
            
            // Tạo các Answers
            if (request.Answers != null && request.Answers.Any())
            {
                foreach (var answerRequest in request.Answers)
                {
                    var answer = new Answers
                    {
                        Order = answerRequest.Order,
                        AnswerContent = answerRequest.AnswerContent,
                        IsCorrect = answerRequest.IsCorrect,
                        CanShuffleAnswer = answerRequest.CanShuffleAnswer,
                        OriginalExamPaperDetailId = questionDetail.OriginalExamPaperDetailId,
                        CreatedBy = userId,
                        CreatedAt = now
                    };
                    
                    await _answersRepository.AddAsync(answer);
                }
            }
            
            // Cập nhật TotalQuestions của OriginalExamPaper
            var totalQuestions = await _originalExamPaperDetailRepository.GetQueryable()
                .CountAsync(q => q.OriginalExamPaperId == request.OriginalExamPaperId && 
                                q.ParentQuestionId == null); // Chỉ đếm câu hỏi độc lập và câu hỏi cha
            
            originalExamPaper.TotalQuestions = totalQuestions;
            originalExamPaper.UpdatedBy = userId;
            originalExamPaper.UpdatedAt = now;
            await _originalExamPaperRepository.UpdateAsync(originalExamPaper);
            
            // Cập nhật KeyValueList với mapping OriginalExamPaperDetailId : AnswerId của đáp án đúng
            await UpdateKeyValueListFromDbAsync(originalExamPaper.OriginalExamPaperId);
            
            // Load lại questionDetail với Answers để trả về
            var questionDetailWithAnswers = await _originalExamPaperDetailRepository.GetQueryable()
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.OriginalExamPaperDetailId == questionDetail.OriginalExamPaperDetailId);
            
            if (questionDetailWithAnswers == null)
                throw new Exception("Lỗi khi tạo câu hỏi");
            
            // Map sang DTO
            var dto = _mapper.Map<OriginalExamPaperDetailDto>(questionDetailWithAnswers);
            dto.Answers = questionDetailWithAnswers.Answers?
                .OrderBy(a => a.Order)
                .Select(a => _mapper.Map<AnswerDto>(a))
                .ToList() ?? new List<AnswerDto>();
            
            return dto;
        }

        public async Task<OriginalExamPaperDetailDto> UpdateQuestionWithAnswersAsync(UpdateQuestionWithAnswersRequest request)
        {
            // Kiểm tra câu hỏi có tồn tại
            var questionDetail = await _originalExamPaperDetailRepository.GetQueryable()
                .Include(q => q.Answers)
                .Include(q => q.OriginalExamPaper)
                .FirstOrDefaultAsync(q => q.OriginalExamPaperDetailId == request.OriginalExamPaperDetailId);
            
            if (questionDetail == null)
                throw new Exception($"Không tìm thấy câu hỏi với ID '{request.OriginalExamPaperDetailId}'");
            
            if (questionDetail.OriginalExamPaper == null)
                throw new Exception("Không tìm thấy đề thi liên quan");
            
            if (!questionDetail.OriginalExamPaper.IsManualCreated)
                throw new Exception("Chỉ có thể sửa câu hỏi cho đề thi được tạo thủ công");
            
            // Kiểm tra ChapterId có tồn tại (nếu có)
            if (request.ChapterId.HasValue)
            {
                var chapter = await _chapterRepository.GetQueryable()
                    .FirstOrDefaultAsync(c => c.ChapterId == request.ChapterId.Value);
                
                if (chapter == null)
                    throw new Exception($"Không tìm thấy chương với ID '{request.ChapterId.Value}'");
                
                // Kiểm tra Chapter thuộc cùng Subject với OriginalExamPaper
                if (chapter.SubjectId != questionDetail.OriginalExamPaper.SubjectId)
                    throw new Exception("Chương không thuộc cùng môn học với đề thi");
            }
            
            // Kiểm tra ParentQuestionId nếu có
            if (request.ParentQuestionId.HasValue)
            {
                var parentQuestion = await _originalExamPaperDetailRepository.GetQueryable()
                    .FirstOrDefaultAsync(q => q.OriginalExamPaperDetailId == request.ParentQuestionId.Value);
                
                if (parentQuestion == null)
                    throw new Exception($"Không tìm thấy câu hỏi cha với ID '{request.ParentQuestionId.Value}'");
                
                if (parentQuestion.OriginalExamPaperId != questionDetail.OriginalExamPaperId)
                    throw new Exception("Câu hỏi cha không thuộc cùng đề thi");
            }
            
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("Không thể xác định người dùng cập nhật câu hỏi.");
            
            var now = DateTimeHelper.GetVietnamTime();
            
            // Cập nhật thông tin câu hỏi
            questionDetail.Order = request.Order;
            questionDetail.QuestionContent = request.QuestionContent;
            questionDetail.CorrectAnswerIndex = request.CorrectAnswerIndex;
            questionDetail.ParentQuestionId = request.ParentQuestionId;
            questionDetail.ChapterId = request.ChapterId;
            questionDetail.CanShuffleQuestion = request.CanShuffleQuestion;
            questionDetail.UpdatedBy = userId;
            questionDetail.UpdatedAt = now;
            
            await _originalExamPaperDetailRepository.UpdateAsync(questionDetail);
            
            // Xử lý câu trả lời
            if (request.Answers != null && request.Answers.Any())
            {
                // Lấy danh sách AnswerId hiện có
                var existingAnswerIds = questionDetail.Answers?
                    .Select(a => a.AnswerId)
                    .ToList() ?? new List<int>();
                
                // Lấy danh sách AnswerId từ request (những câu trả lời cần giữ lại)
                var requestAnswerIds = request.Answers
                    .Where(a => a.AnswerId.HasValue)
                    .Select(a => a.AnswerId.Value)
                    .ToList();
                
                // Xóa những câu trả lời không còn trong request
                var answersToDelete = existingAnswerIds
                    .Where(id => !requestAnswerIds.Contains(id))
                    .ToList();
                
                foreach (var answerIdToDelete in answersToDelete)
                {
                    var answerToDelete = await _answersRepository.GetQueryable()
                        .FirstOrDefaultAsync(a => a.AnswerId == answerIdToDelete);
                    
                    if (answerToDelete != null)
                    {
                        await _answersRepository.DeleteAsync(answerToDelete);
                    }
                }
                
                // Cập nhật hoặc thêm mới câu trả lời
                foreach (var answerRequest in request.Answers)
                {
                    if (answerRequest.AnswerId.HasValue)
                    {
                        // Cập nhật câu trả lời hiện có
                        var existingAnswer = await _answersRepository.GetQueryable()
                            .FirstOrDefaultAsync(a => a.AnswerId == answerRequest.AnswerId.Value);
                        
                        if (existingAnswer != null)
                        {
                            existingAnswer.Order = answerRequest.Order;
                            existingAnswer.AnswerContent = answerRequest.AnswerContent;
                            existingAnswer.IsCorrect = answerRequest.IsCorrect;
                            existingAnswer.CanShuffleAnswer = answerRequest.CanShuffleAnswer;
                            existingAnswer.UpdatedBy = userId;
                            existingAnswer.UpdatedAt = now;
                            
                            await _answersRepository.UpdateAsync(existingAnswer);
                        }
                    }
                    else
                    {
                        // Thêm câu trả lời mới
                        var newAnswer = new Answers
                        {
                            Order = answerRequest.Order,
                            AnswerContent = answerRequest.AnswerContent,
                            IsCorrect = answerRequest.IsCorrect,
                            CanShuffleAnswer = answerRequest.CanShuffleAnswer,
                            OriginalExamPaperDetailId = questionDetail.OriginalExamPaperDetailId,
                            CreatedBy = userId,
                            CreatedAt = now
                        };
                        
                        await _answersRepository.AddAsync(newAnswer);
                    }
                }
            }
            
            // Load lại questionDetail với Answers để trả về
            var updatedQuestionDetail = await _originalExamPaperDetailRepository.GetQueryable()
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.OriginalExamPaperDetailId == questionDetail.OriginalExamPaperDetailId);
            
            if (updatedQuestionDetail == null)
                throw new Exception("Lỗi khi cập nhật câu hỏi");
            
            // Cập nhật KeyValueList sau khi thay đổi đáp án/câu hỏi
            await UpdateKeyValueListFromDbAsync(questionDetail.OriginalExamPaperId);
            
            // Map sang DTO
            var dto = _mapper.Map<OriginalExamPaperDetailDto>(updatedQuestionDetail);
            dto.Answers = updatedQuestionDetail.Answers?
                .OrderBy(a => a.Order)
                .Select(a => _mapper.Map<AnswerDto>(a))
                .ToList() ?? new List<AnswerDto>();
            
            return dto;
        }

        /// <summary>
        /// Regenerate KeyValueList string in format "(QuestionId:AnswerId);..." using current correct answers.
        /// </summary>
        private async Task UpdateKeyValueListFromDbAsync(int originalExamPaperId)
        {
            var examPaper = await _originalExamPaperRepository.GetQueryable()
                .Include(o => o.OriginalExamPaperDetails)
                    .ThenInclude(d => d.Answers)
                .FirstOrDefaultAsync(o => o.OriginalExamPaperId == originalExamPaperId);

            if (examPaper == null)
            {
                return;
            }

            var keyValuePairs = examPaper.OriginalExamPaperDetails
                .OrderBy(d => d.OriginalExamPaperDetailId)
                .Select(detail =>
                {
                    var correctAnswerId = detail.Answers?
                        .FirstOrDefault(a => a.IsCorrect)?
                        .AnswerId;

                    return correctAnswerId.HasValue
                        ? $"({detail.OriginalExamPaperDetailId}:{correctAnswerId.Value})"
                        : null;
                })
                .Where(pair => pair != null)
                .ToList();

            examPaper.KeyValueList = keyValuePairs.Any()
                ? string.Join(";", keyValuePairs) + ";"
                : null;

            await _originalExamPaperRepository.UpdateAsync(examPaper);
        }

        public async Task<string> GenerateRandomOriginalExamPaperCoreAsync()
        {
            const string uppercaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const int codeLength = 26;
            const int maxAttempts = 100; // Giới hạn số lần thử để tránh vòng lặp vô hạn
            
            var random = new Random();
            int attempts = 0;
            string generatedCode;
            
            do
            {
                // Tạo mã ngẫu nhiên 26 chữ cái in hoa
                generatedCode = new string(Enumerable.Range(0, codeLength)
                    .Select(_ => uppercaseLetters[random.Next(uppercaseLetters.Length)])
                    .ToArray());
                
                // Kiểm tra xem mã đã tồn tại chưa
                var exists = await _originalExamPaperRepository.GetQueryable()
                    .AnyAsync(x => x.OriginalExamPaperCore == generatedCode);
                
                if (!exists)
                {
                    return generatedCode;
                }
                
                attempts++;
                
                // Nếu đã thử quá nhiều lần, throw exception
                if (attempts >= maxAttempts)
                {
                    throw new Exception("Không thể tạo mã OriginalExamPaperCore duy nhất sau nhiều lần thử. Vui lòng thử lại.");
                }
            } while (true);
        }

        /// <summary>
        /// Xóa cứng đề thi gốc và tất cả các đề hoán vị liên quan
        /// Xóa vĩnh viễn khỏi database (hard delete)
        /// </summary>
        /// <param name="originalExamPaperId">ID của đề thi gốc cần xóa</param>
        /// <returns>True nếu xóa thành công, False nếu không tìm thấy đề thi</returns>
        public async Task<bool> HardDeleteAsync(int originalExamPaperId)
        {
            try
            {
                // Lấy đề thi gốc từ database, bao gồm các đề hoán vị liên quan
                var originalExamPaper = await _originalExamPaperRepository.GetQueryable()
                    .Include(oep => oep.ShuffledExamPapers)
                    .FirstOrDefaultAsync(oep => oep.OriginalExamPaperId == originalExamPaperId);

                if (originalExamPaper == null)
                {
                    return false; // Không tìm thấy đề thi gốc
                }

                // Xóa tất cả các đề hoán vị liên quan trước
                // Lấy danh sách tất cả ShuffledExamPaper có OriginalExamPaperId = originalExamPaperId
                var shuffledPapers = await _shuffledExamPaperRepository.GetQueryable()
                    .Where(sep => sep.OriginalExamPaperId == originalExamPaperId)
                    .ToListAsync();

                foreach (var shuffledPaper in shuffledPapers)
                {
                    // Xóa cứng đề hoán vị (hard delete)
                    await _shuffledExamPaperRepository.DeleteAsync(shuffledPaper.ShuffledExamPaperId);
                }

                // Xóa cứng đề thi gốc (hard delete)
                await _originalExamPaperRepository.DeleteAsync(originalExamPaperId);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa cứng đề thi gốc ID {originalExamPaperId}: {ex.Message}", ex);
            }
        }

        
    }
}
