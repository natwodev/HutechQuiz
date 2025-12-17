using Microsoft.AspNetCore.Components;
using frontend_manage.DTOs;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.ExamManager;
using frontend_manage.Services;
using MudBlazor;

namespace frontend_manage.Pages.ExamManager.Preview
{
    public partial class OriginalExamPaperPreviewPage : ComponentBase
    {
        [Parameter]
        [SupplyParameterFromQuery]
        public string? core { get; set; }

        [Inject]
        private ExamManagerService ExamManagerService { get; set; } = null!;

        [Inject]
        private IKaTeXService KaTeX { get; set; } = null!;

        [Inject]
        private IQrCodeService QRCodeService { get; set; } = null!;

        private OriginalExamPaperDto? Exam;
        private bool isLoading = true;
        private bool isUpdating = false;
        private bool isEditMode = false;
        private UpdateOriginalExamPaperRequest editRequest = new();
        private List<SubjectDto> subjects = new();
        
        // Thêm câu hỏi
        private bool isAddQuestionMode = false;
        private CreateQuestionWithAnswersRequest questionRequest = new();
        private bool isAddingQuestion = false;
        private string? questionMessage;

        // Sửa câu hỏi
        private int? editingQuestionId = null;
        private UpdateQuestionWithAnswersRequest editQuestionRequest = new();
        private bool isUpdatingQuestion = false;
        private string? editQuestionMessage;

        private bool IsAddQuestionButtonDisabled => isUpdating || Exam == null || isAddQuestionMode || isEditMode || editingQuestionId.HasValue;

        // QR Code display
        private bool showQRCode = false;
        private string? qrCodeBase64 = null;

        private async Task OnAllowViewMaterialsChanged(bool newValue)
        {
            await HandleToggleAllowViewMaterials(newValue);
        }

        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrWhiteSpace(core))
            {
                isLoading = true;
                try
                {
                    Exam = await ExamManagerService.GetOriginalExamWithDetailsAsync(core);
                    subjects = await ExamManagerService.GetAllSubjectsAsync();
                }
                catch (Exception ex)
                {
                    // Handle error
                }
                finally
                {
                    isLoading = false;
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await KaTeX.RenderAsync(".katex-content");
            }
        }

        private string RenderHtml(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;
            
            // Loại bỏ các ký tự {<number>} khỏi nội dung
            return System.Text.RegularExpressions.Regex.Replace(
                content, 
                @"\{<\d+>\}", 
                string.Empty
            );
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

        private int GetTotalQuestionsCount()
        {
            if (Exam?.Details == null || Exam.Details.Count == 0)
                return 0;

            int count = 0;
            foreach (var q in Exam.Details.Where(d => d.ParentQuestionId == null))
            {
                var isParentQuestion = q.ChildQuestions != null && q.ChildQuestions.Count > 0;
                
                if (isParentQuestion)
                {
                    // Câu hỏi cha: chỉ đếm các câu hỏi con
                    if (q.ChildQuestions != null && q.ChildQuestions.Count > 0)
                    {
                        count += q.ChildQuestions.Count;
                    }
                }
                else
                {
                    // Câu hỏi độc lập: đếm chính nó
                    count++;
                }
            }
            return count;
        }

        private async Task HandleToggleAllowViewMaterials(bool newValue)
        {
            if (Exam == null || string.IsNullOrWhiteSpace(Exam.OriginalExamPaperCore))
                return;

            var oldValue = Exam.AllowViewMaterials;
            
            // Optimistic update
            Exam.AllowViewMaterials = newValue;
            isUpdating = true;
            StateHasChanged();

            try
            {
                // Sử dụng API update mới để cập nhật toàn bộ thông tin đề thi
                var updateRequest = new UpdateOriginalExamPaperRequest
                {
                    OriginalExamPaperCore = Exam.OriginalExamPaperCore,
                    Title = Exam.Title,
                    Description = Exam.Description,
                    SubjectId = Exam.SubjectId,
                    AllowViewMaterials = newValue,
                    DurationMinutes = Exam.DurationMinutes,
                    IsApproved = Exam.IsApproved
                };
                
                var updatedExam = await ExamManagerService.UpdateOriginalExamPaperAsync(updateRequest);
                if (updatedExam != null)
                {
                    Exam = updatedExam;
                }
            }
            catch (Exception ex)
            {
                // Revert on error
                Exam.AllowViewMaterials = oldValue;
                System.Diagnostics.Debug.WriteLine($"Lỗi khi cập nhật đề thi: {ex.Message}");
                // Có thể hiển thị thông báo lỗi cho người dùng ở đây
            }
            finally
            {
                isUpdating = false;
                StateHasChanged();
            }
        }

        private void ToggleEditMode()
        {
            if (Exam == null)
                return;

            // Nếu đang ở chế độ thêm câu hỏi, đóng nó trước
            if (isAddQuestionMode)
            {
                isAddQuestionMode = false;
                questionRequest = new CreateQuestionWithAnswersRequest();
                questionMessage = null;
            }

            if (!isEditMode)
            {
                // Bật chế độ chỉnh sửa, khởi tạo form với dữ liệu hiện tại
                editRequest = new UpdateOriginalExamPaperRequest
                {
                    OriginalExamPaperCore = Exam.OriginalExamPaperCore,
                    Title = Exam.Title,
                    Description = Exam.Description,
                    SubjectId = Exam.SubjectId,
                    AllowViewMaterials = Exam.AllowViewMaterials,
                    DurationMinutes = Exam.DurationMinutes,
                    IsApproved = Exam.IsApproved
                };
            }
            
            isEditMode = !isEditMode;
            StateHasChanged();
        }

        private async Task HandleSave()
        {
            if (Exam == null || string.IsNullOrWhiteSpace(editRequest.OriginalExamPaperCore))
                return;

            if (string.IsNullOrWhiteSpace(editRequest.Title))
            {
                System.Diagnostics.Debug.WriteLine("Tiêu đề không được để trống");
                return;
            }

            isUpdating = true;
            StateHasChanged();

            try
            {
                var updatedExam = await ExamManagerService.UpdateOriginalExamPaperAsync(editRequest);
                if (updatedExam != null)
                {
                    Exam = updatedExam;
                    isEditMode = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi cập nhật đề thi: {ex.Message}");
                // Có thể hiển thị thông báo lỗi cho người dùng ở đây
            }
            finally
            {
                isUpdating = false;
                StateHasChanged();
            }
        }

        private string GetSubjectName(int subjectId)
        {
            var subject = subjects.FirstOrDefault(s => s.SubjectId == subjectId);
            return subject != null ? subject.SubjectName : "N/A";
        }

        private string GetApprovalStatusText(bool? isApproved)
        {
            if (isApproved == true)
                return "Đã phê duyệt";
            else if (isApproved == false)
                return "Chưa phê duyệt";
            else
                return "Chưa xác định";
        }

        private string GetApprovalStatusClass(bool? isApproved)
        {
            if (isApproved == true)
                return "status-approved";
            else if (isApproved == false)
                return "status-pending";
            else
                return "status-unknown";
        }

        private Color GetApprovalStatusColor(bool? isApproved)
        {
            if (isApproved == true)
                return Color.Success;
            else if (isApproved == false)
                return Color.Warning;
            else
                return Color.Default;
        }

        private string? GetIsApprovedString(bool? isApproved)
        {
            if (isApproved == true)
                return "true";
            else if (isApproved == false)
                return "false";
            else
                return null;
        }

        private void SetIsApprovedFromString(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                editRequest.IsApproved = null;
            }
            else if (bool.TryParse(value, out bool result))
            {
                editRequest.IsApproved = result;
            }
        }

        // Methods cho thêm câu hỏi
        private void ToggleAddQuestionMode()
        {
            if (Exam == null)
                return;

            // Nếu đang ở chế độ chỉnh sửa, đóng nó trước
            if (isEditMode)
            {
                isEditMode = false;
                editRequest = new UpdateOriginalExamPaperRequest();
            }

            // Nếu đang sửa câu hỏi, đóng nó trước
            if (editingQuestionId.HasValue)
            {
                CloseEditQuestion();
            }

            if (!isAddQuestionMode)
            {
                // Bật chế độ thêm câu hỏi, khởi tạo form
                questionRequest = new CreateQuestionWithAnswersRequest
                {
                    OriginalExamPaperId = Exam.OriginalExamPaperId,
                    Order = GetTotalQuestionsCount() + 1,
                    QuestionContent = string.Empty,
                    CanShuffleQuestion = true,
                    Answers = new List<CreateAnswerRequest>
                    {
                        new CreateAnswerRequest { Order = 1, AnswerContent = string.Empty, IsCorrect = false, CanShuffleAnswer = true },
                        new CreateAnswerRequest { Order = 2, AnswerContent = string.Empty, IsCorrect = false, CanShuffleAnswer = true }
                    }
                };
                questionMessage = null;
            }
            else
            {
                // Đóng form, reset dữ liệu
                questionRequest = new CreateQuestionWithAnswersRequest();
                questionMessage = null;
            }
            
            isAddQuestionMode = !isAddQuestionMode;
            StateHasChanged();
        }

        private void AddAnswer()
        {
            var newOrder = questionRequest.Answers.Count + 1;
            questionRequest.Answers.Add(new CreateAnswerRequest 
            { 
                Order = newOrder, 
                AnswerContent = string.Empty, 
                IsCorrect = false,
                CanShuffleAnswer = true
            });
            StateHasChanged();
        }

        private void RemoveAnswer(int index)
        {
            if (questionRequest.Answers.Count > 2)
            {
                questionRequest.Answers.RemoveAt(index);
                // Cập nhật lại Order
                for (int i = 0; i < questionRequest.Answers.Count; i++)
                {
                    questionRequest.Answers[i].Order = i + 1;
                }
                StateHasChanged();
            }
        }

        private async Task AddQuestionAsync()
        {
            if (Exam == null)
                return;

            // Đảm bảo Order của các đáp án được set đúng
            for (int i = 0; i < questionRequest.Answers.Count; i++)
            {
                questionRequest.Answers[i].Order = i + 1;
            }

            isAddingQuestion = true;
            questionMessage = null;
            StateHasChanged();

            try
            {
                var result = await ExamManagerService.AddQuestionWithAnswersAsync(questionRequest);
                
                if (result != null)
                {
                    questionMessage = "Thêm câu hỏi thành công!";
                    
                    // Reload đề thi để hiển thị câu hỏi mới
                    if (!string.IsNullOrWhiteSpace(core))
                    {
                        Exam = await ExamManagerService.GetOriginalExamWithDetailsAsync(core);
                        await KaTeX.RenderAsync(".katex-content");
                    }
                    
                    // Đóng form sau 1 giây
                    await Task.Delay(1000);
                    isAddQuestionMode = false;
                    questionRequest = new CreateQuestionWithAnswersRequest();
                    questionMessage = null;
                }
            }
            catch (Exception ex)
            {
                questionMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                isAddingQuestion = false;
                StateHasChanged();
            }
        }

        // Methods cho sửa câu hỏi
        private void OpenEditQuestion(int questionId)
        {
            if (Exam == null)
                return;

            // Đóng form thêm câu hỏi nếu đang mở
            if (isAddQuestionMode)
            {
                isAddQuestionMode = false;
                questionRequest = new CreateQuestionWithAnswersRequest();
                questionMessage = null;
            }

            // Đóng form chỉnh sửa đề thi nếu đang mở
            if (isEditMode)
            {
                isEditMode = false;
                editRequest = new UpdateOriginalExamPaperRequest();
            }

            // Tìm câu hỏi cần sửa
            var question = GetFlatQuestions().FirstOrDefault(q => q.OriginalExamPaperDetailId == questionId);
            if (question == null)
                return;

            // Khởi tạo form với dữ liệu hiện tại
            editQuestionRequest = new UpdateQuestionWithAnswersRequest
            {
                OriginalExamPaperDetailId = question.OriginalExamPaperDetailId,
                Order = question.Order,
                QuestionContent = question.QuestionContent,
                CorrectAnswerIndex = question.CorrectAnswerIndex,
                ParentQuestionId = question.ParentQuestionId,
                ChapterId = question.ChapterId,
                CanShuffleQuestion = question.CanShuffleQuestion,
                Answers = question.Answers?.Select(a => new UpdateAnswerRequest
                {
                    AnswerId = a.AnswerId,
                    Order = a.Order,
                    AnswerContent = a.AnswerContent ?? string.Empty,
                    IsCorrect = a.IsCorrect,
                    CanShuffleAnswer = a.CanShuffleAnswer
                }).ToList() ?? new List<UpdateAnswerRequest>()
            };

            // Đảm bảo có ít nhất 2 đáp án
            if (editQuestionRequest.Answers.Count < 2)
            {
                while (editQuestionRequest.Answers.Count < 2)
                {
                    editQuestionRequest.Answers.Add(new UpdateAnswerRequest
                    {
                        Order = editQuestionRequest.Answers.Count + 1,
                        AnswerContent = string.Empty,
                        IsCorrect = false,
                        CanShuffleAnswer = true
                    });
                }
            }

            editingQuestionId = questionId;
            editQuestionMessage = null;
            StateHasChanged();
        }

        private void CloseEditQuestion()
        {
            editingQuestionId = null;
            editQuestionRequest = new UpdateQuestionWithAnswersRequest();
            editQuestionMessage = null;
            StateHasChanged();
        }

        private void AddEditAnswer()
        {
            var newOrder = editQuestionRequest.Answers.Count + 1;
            editQuestionRequest.Answers.Add(new UpdateAnswerRequest
            {
                Order = newOrder,
                AnswerContent = string.Empty,
                IsCorrect = false,
                CanShuffleAnswer = true
            });
            StateHasChanged();
        }

        private void RemoveEditAnswer(int index)
        {
            if (editQuestionRequest.Answers.Count > 2)
            {
                editQuestionRequest.Answers.RemoveAt(index);
                // Cập nhật lại Order
                for (int i = 0; i < editQuestionRequest.Answers.Count; i++)
                {
                    editQuestionRequest.Answers[i].Order = i + 1;
                }
                StateHasChanged();
            }
        }

        private async Task UpdateQuestionAsync()
        {
            if (Exam == null || !editingQuestionId.HasValue)
                return;

            // Đảm bảo Order của các đáp án được set đúng
            for (int i = 0; i < editQuestionRequest.Answers.Count; i++)
            {
                editQuestionRequest.Answers[i].Order = i + 1;
            }

            isUpdatingQuestion = true;
            editQuestionMessage = null;
            StateHasChanged();

            try
            {
                var result = await ExamManagerService.UpdateQuestionWithAnswersAsync(editQuestionRequest);

                if (result != null)
                {
                    editQuestionMessage = "Cập nhật câu hỏi thành công!";

                    // Reload đề thi để hiển thị câu hỏi đã cập nhật
                    if (!string.IsNullOrWhiteSpace(core))
                    {
                        Exam = await ExamManagerService.GetOriginalExamWithDetailsAsync(core);
                        await KaTeX.RenderAsync(".katex-content");
                    }

                    // Đóng form sau 1 giây
                    await Task.Delay(1000);
                    CloseEditQuestion();
                }
            }
            catch (Exception ex)
            {
                editQuestionMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                isUpdatingQuestion = false;
                StateHasChanged();
            }
        }

        private void DeleteQuestion(int questionId)
        {
            // Chưa phát triển
            // Có thể hiển thị dialog xác nhận xóa ở đây
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

                // Lấy tên môn học từ danh sách subjects, fallback theo SubjectId
                var subjectName = subjects
                    .FirstOrDefault(s => s.SubjectId == Exam.SubjectId)?
                    .SubjectName;
                if (string.IsNullOrWhiteSpace(subjectName))
                {
                    subjectName = $"Mon_{Exam.SubjectId}";
                }
                subjectName = SanitizeForQr(subjectName);

                var durationMinutes = Exam.DurationMinutes;
                var createdAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");

                // Định dạng cố định: EXAM|v=1|key=value|...
                var qrText =
                    $"EXAM|v=1|id={Exam.OriginalExamPaperId}|core={Exam.OriginalExamPaperCore}|title={title}|desc={description}|sub={subjectName}|dur={durationMinutes}|created={createdAt}";

                // Tạo QR code với nội dung định dạng cố định (pixelsPerModule nhỏ hơn để mã gọn hơn)
                qrCodeBase64 = QRCodeService.GenerateQrCodeAsBase64(qrText, 6);
            }
            
            showQRCode = !showQRCode;
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
}
