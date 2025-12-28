using Microsoft.AspNetCore.Components;
using MudBlazor;
using frontend_manage.Services;
using frontend_manage.DTOs;
using frontend_manage.Services.ExamManager;
using System.Linq;
using Microsoft.JSInterop;

namespace frontend_manage.Pages.Monitor.Components;

public partial class QuickTestTab : ComponentBase
{
    [Inject] private MonitorService MonitorService { get; set; } = default!;

    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Inject] private ExamManagerService ExamManagerService { get; set; } = default!;

    [Inject] private IKaTeXService KaTeX { get; set; } = default!;

    [Inject] private IQrCodeService QRCodeService { get; set; } = default!;

    [Inject] private IExamRenderingService ExamRenderingService { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter] public SubjectExamRoomStatusDto? Subject { get; set; }

    [Parameter] public int? ExamSessionSubjectId { get; set; }

    private OriginalExamPaperDto? Exam;
    private bool isLoadingExam = false;
    private string? examLoadError = null;
    private string? lastLoadedCore = null;
    
    // QR Code display
    private bool showQRCode = false;
    private string? qrCodeBase64 = null;

    protected override async Task OnParametersSetAsync()
    {
        // Chỉ load lại nếu OriginalExamPaperCore thay đổi
        var currentCore = Subject?.OriginalExamPaperCore;
        if (currentCore != lastLoadedCore)
        {
            lastLoadedCore = currentCore;
            await LoadExamPreview();
        }
    }

    private async Task LoadExamPreview()
    {
        if (Subject == null || string.IsNullOrWhiteSpace(Subject.OriginalExamPaperCore))
        {
            Exam = null;
            examLoadError = null;
            lastLoadedCore = null;
            StateHasChanged();
            return;
        }

        var coreToLoad = Subject.OriginalExamPaperCore;
        isLoadingExam = true;
        examLoadError = null;
        StateHasChanged();
        
        try
        {
            System.Diagnostics.Debug.WriteLine($"Loading exam with core: {coreToLoad}");
            Exam = await ExamManagerService.GetOriginalExamWithDetailsAsync(coreToLoad);
            
            // Log để debug
            if (Exam == null)
            {
                examLoadError = "Không tìm thấy đề thi với mã: " + coreToLoad;
                System.Diagnostics.Debug.WriteLine($"Exam is null for core: {coreToLoad}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Exam loaded. OriginalExamPaperId: {Exam.OriginalExamPaperId}, Details count: {Exam.Details?.Count ?? 0}");
                
                if (Exam.Details == null || Exam.Details.Count == 0)
                {
                    examLoadError = "Đề thi không có câu hỏi";
                    System.Diagnostics.Debug.WriteLine($"Exam has no details. Details is null: {Exam.Details == null}");
                }
                else
                {
                    examLoadError = null;
                    System.Diagnostics.Debug.WriteLine($"Exam loaded successfully. Details count: {Exam.Details.Count}");
                    
                    // Log thêm thông tin về câu hỏi
                    var flatQuestions = GetFlatQuestions();
                    System.Diagnostics.Debug.WriteLine($"Flat questions count: {flatQuestions.Count}");
                }
            }
            
            lastLoadedCore = coreToLoad;
            StateHasChanged();
            
            // Render KaTeX sau khi đã render UI
            await Task.Delay(100);
            if (Exam != null && Exam.Details != null && Exam.Details.Count > 0)
            {
                await KaTeX.RenderAsync(".katex-content");
            }
        }
        catch (Exception ex)
        {
            Exam = null;
            examLoadError = $"Lỗi khi tải đề thi: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Lỗi khi tải đề thi: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            StateHasChanged();
        }
        finally
        {
            isLoadingExam = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Exam != null)
        {
            await KaTeX.RenderAsync(".katex-content");
            await JS.InvokeVoidAsync("replaceAudioWithCustomControls");
        }
    }

    private string RenderHtml(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        // Sử dụng ExamRenderingService để normalize và render content (bao gồm latex, audio, image)
        return ExamRenderingService.NormalizeAndRenderContent(content, null, Exam?.OriginalExamPaperCore);
    }
    
    private char GetLetter(int order) => (char)('A' + Math.Max(0, order - 1));

    private List<OriginalExamPaperDetailDto> GetFlatQuestions()
    {
        if (Exam?.Details == null || Exam.Details.Count == 0)
            return new List<OriginalExamPaperDetailDto>();

        var flatQuestions = new List<OriginalExamPaperDetailDto>();
        var parentQuestions = Exam.Details.Where(d => d.ParentQuestionId == null).OrderBy(d => d.Order).ToList();
        
        foreach (var parentQ in parentQuestions)
        {
            // Nếu là câu hỏi cha (có child questions)
            if (parentQ.ChildQuestions != null && parentQ.ChildQuestions.Count > 0)
            {
                flatQuestions.Add(parentQ); // Thêm câu hỏi cha
                // Thêm các câu hỏi con
                foreach (var child in parentQ.ChildQuestions.OrderBy(c => c.Order))
                {
                    flatQuestions.Add(child);
                }
            }
            // Nếu là câu hỏi độc lập (không có child)
            else
            {
                flatQuestions.Add(parentQ);
            }
        }

        return flatQuestions;
    }

    private int? GetQuestionNumber(OriginalExamPaperDetailDto question, int indexInFlatList)
    {
        // Nếu là parent question (có child questions), không đánh số
        if (question.ParentQuestionId == null && question.ChildQuestions != null && question.ChildQuestions.Count > 0)
        {
            return null;
        }

        // Tính số thứ tự dựa trên các câu hỏi trước đó
        int number = 1;
        var flatQuestions = GetFlatQuestions();
        
        for (int i = 0; i < indexInFlatList; i++)
        {
            var q = flatQuestions[i];
            // Chỉ đếm các câu hỏi không phải parent question
            var isParent = q.ParentQuestionId == null && q.ChildQuestions != null && q.ChildQuestions.Count > 0;
            if (!isParent)
            {
                number++;
            }
        }
        
        return number;
    }

    private void ToggleQRCodeDisplay()
    {
        if (Exam == null || string.IsNullOrWhiteSpace(Exam.OriginalExamPaperCore))
            return;

        if (!showQRCode)
        {
            // Chuẩn bị nội dung QR theo định dạng cố định, dễ validate
            var title = SanitizeForQr(Exam.Title);
            var description = SanitizeForQr(Exam.Description);

            // Lấy thông tin môn học và thời gian từ Subject (nếu có), fallback sang Exam
            var subjectName = Subject?.SubjectName;
            if (string.IsNullOrWhiteSpace(subjectName))
            {
                subjectName = $"Mon_{Exam.SubjectId}";
            }
            subjectName = SanitizeForQr(subjectName);

            var durationMinutes = Subject?.Duration > 0 ? Subject.Duration : Exam.DurationMinutes;

            var createdAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");

            // Định dạng cố định: EXAM|v=1|key=value|...
            var qrText =
                $"EXAM|v=1|id={Exam.OriginalExamPaperId}|core={Exam.OriginalExamPaperCore}|title={title}|desc={description}|sub={subjectName}|dur={durationMinutes}|created={createdAt}";

            // Tạo QR code với nội dung định dạng cố định
            // pixelsPerModule nhỏ hơn để QR hiển thị gọn hơn
            qrCodeBase64 = QRCodeService.GenerateQrCodeAsBase64(qrText, 6);
        }
        
        showQRCode = !showQRCode;
        StateHasChanged();
    }

    private static string SanitizeForQr(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        // Loại bỏ ký tự gây vỡ cấu trúc (|, =) và chuẩn hóa khoảng trắng thành _
        var sanitized = value.Replace("|", " ")
                             .Replace("=", " ")
                             .Replace("\r", " ")
                             .Replace("\n", " ")
                             .Trim();
        sanitized = string.Join("_", sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return sanitized;
    }
}
