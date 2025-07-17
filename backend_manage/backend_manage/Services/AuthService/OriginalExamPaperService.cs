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

namespace backend_manage.Services.AuthService
{
    public class OriginalExamPaperService : IOriginalExamPaperService
    {
        private readonly IRepository<Subject> _subjectRepository;
        private readonly IRepository<OriginalExamPaper> _originalExamPaperRepository;
        private readonly IRepository<Chapter> _chapterRepository;
        private readonly IRepository<OriginalExamPaperDetail> _originalExamPaperDetailRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public OriginalExamPaperService(
            IRepository<Subject> subjectRepository,
            IRepository<OriginalExamPaper> originalExamPaperRepository,
            IRepository<Chapter> chapterRepository,
            IRepository<OriginalExamPaperDetail> originalExamPaperDetailRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _subjectRepository = subjectRepository;
            _originalExamPaperRepository = originalExamPaperRepository;
            _chapterRepository = chapterRepository;
            _originalExamPaperDetailRepository = originalExamPaperDetailRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        // Pass giải nén file XML
        public const string ExtractPassword = "649224E2-F0AC-42B1-AD1B-2EAF04E2AC7D-FE602240-7E60-43BF-828D-D6AF38A70429-52572FD1-BB94-45AD-95CF-7B2B5C2E85A6-1B3D4CCF-808E-4ABF-8F9E-73ADF041C78B";

        public async Task ImportFromXmlAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ hoặc rỗng");
            if (!file.FileName.EndsWith(".epz", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("File phải có đuôi .epz");

            string xmlContent = null;
            using (var zipStream = new MemoryStream())
            {
                await file.CopyToAsync(zipStream);
                zipStream.Position = 0;
                using (var zipFile = new ICSharpCode.SharpZipLib.Zip.ZipFile(zipStream))
                {
                    zipFile.Password = ExtractPassword; // Sử dụng pass giải nén
                    foreach (ZipEntry entry in zipFile)
                    {
                        if (entry.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var entryStream = zipFile.GetInputStream(entry))
                            using (var reader = new StreamReader(entryStream))
                            {
                                xmlContent = await reader.ReadToEndAsync();
                            }
                            break;
                        }
                    }
                }
            }
            if (xmlContent == null)
                throw new Exception("Không tìm thấy file XML trong archive");

            var serializer = new XmlSerializer(typeof(EPZDto));
            EPZDto epz;
            using (var reader = new StringReader(xmlContent))
            {
                epz = (EPZDto)serializer.Deserialize(reader);
            }
            var monHoc = epz.MonHoc;
            if (monHoc == null) throw new Exception("XML không hợp lệ: thiếu MonHoc");
            // Kiểm tra SubjectCore đã tồn tại chưa
            var exists = await _subjectRepository.GetQueryable().AnyAsync(s => s.SubjectCore == monHoc.MaSoMonHoc);
            Subject subject = null;
            var now = DateTimeHelper.GetVietnamTime(); // Avoid duplicate calls
            var userIdForExamPaper = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdForExamPaper))
                throw new UnauthorizedAccessException("Không thể xác định người dùng tạo đề thi gốc.");
            if (!exists)
            {
                if (string.IsNullOrEmpty(userIdForExamPaper))
                    throw new UnauthorizedAccessException("Không thể xác định người dùng tạo sinh viên.");
                subject = new Subject
                {
                    SubjectCore = monHoc.MaSoMonHoc,
                    SubjectName = monHoc.TenMonHoc,
                    DepartmentId = null, // Tạm thời bỏ qua
                    CreatedBy = userIdForExamPaper,
                    CreatedAt = now
                };
                await _subjectRepository.AddAsync(subject);
            }
            else
            {
                subject = await _subjectRepository.GetQueryable().FirstOrDefaultAsync(s => s.SubjectCore == monHoc.MaSoMonHoc);
            }

            // Save OriginalExamPaper (phải tạo trước để lấy Id cho detail)
            var originalExamPaper = new OriginalExamPaper
            {
                Title = monHoc.TenMonHoc ?? "Đề thi gốc", // Dùng tên môn học làm tiêu đề đề thi
                Description = null, // TODO: Bổ sung nếu có trường mô tả trong XML
                SubjectId = subject.SubjectId,
                CreatedBy = userIdForExamPaper,
                CreatedAt = now,
                DurationMinutes = 0, // TODO: Bổ sung nếu có trường thời gian làm bài trong XML
                TotalQuestions = monHoc.TongSoCauLay > 0 ? monHoc.TongSoCauLay : 0,
                OriginalExamPaperCore = "mã đề gốc khi thêm vào hệ thống thi sau"
            };
            await _originalExamPaperRepository.AddAsync(originalExamPaper);

            // Lưu các phần (section) dựa vào TenPhan
            if (monHoc.Phan != null)
            {
                foreach (var phan in monHoc.Phan)
                {
                    int? parentChapterId = null;
                    if (!string.IsNullOrEmpty(phan.MaPhanCha) && phan.MaPhanCha != "00000000-0000-0000-0000-000000000000")
                    {
                        // Tìm TenPhan của parent section dựa vào MaPhanCha
                        var parentPhan = monHoc.Phan.FirstOrDefault(p => p.MaPhan == phan.MaPhanCha);
                        if (parentPhan != null)
                        {
                            var parentChapter = await _chapterRepository.GetQueryable()
                                .FirstOrDefaultAsync(c => c.Name == parentPhan.TenPhan && c.SubjectId == subject.SubjectId);
                            if (parentChapter != null)
                            {
                                parentChapterId = parentChapter.ChapterId;
                            }
                        }
                    }
                    // Tìm hoặc tạo Chapter theo TenPhan
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
                            Order = 0, // TODO: lấy thứ tự nếu có
                            IsGroupQuestion = false, // TODO: xác định nếu có logic nhóm
                            ParentChapterId = parentChapterId
                        };
                        await _chapterRepository.AddAsync(chapter);
                    }
                    // Lưu câu hỏi và OriginalExamPaperDetail cho từng phần (phan.CauHoi)
                    if (phan.CauHoi != null)
                    {
                        int order = 1;
                        foreach (var cauHoi in phan.CauHoi)
                        {
                            // Lưu chi tiết đề thi gốc (chỉ lưu nội dung, không còn liên kết Question/Answer)
                            var detail = new OriginalExamPaperDetail
                            {
                                OriginalExamPaperId = originalExamPaper.OriginalExamPaperId,
                                ChapterId = chapter.ChapterId,
                                Order = order,
                                CreatedBy = userIdForExamPaper,
                                CreatedAt = now,
                                // Nếu entity có trường QuestionContent/AnswersJson thì lưu ở đây, nếu không thì chỉ lưu các trường hiện có
                            };
                            await _originalExamPaperDetailRepository.AddAsync(detail);
                            order++;
                        }
                    }
                }
            }
        }
    }
} 