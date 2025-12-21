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

        [Inject]
        private IExamRenderingService ExamRenderingService { get; set; } = null!;

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
                    StateHasChanged(); // Trigger re-render để KaTeX có thể render content mới
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Render KaTeX mỗi lần component render để đảm bảo các elements mới được xử lý
            // Đặc biệt quan trọng khi data được load async sau khi component đã render
            try
        {
            if (firstRender)
            {
                    // Sử dụng RenderWithRetryAsync cho first render để đảm bảo KaTeX đã sẵn sàng
                    await KaTeX.RenderWithRetryAsync(".katex-content", maxRetries: 3, delayMs: 100);
                }
                else
                {
                    // Re-render khi content thay đổi (ví dụ: sau khi load data, thêm/sửa câu hỏi)
                await KaTeX.RenderAsync(".katex-content");
                }
            }
            catch (Exception ex)
            {
                // Log error nhưng không throw để không làm gián đoạn UI
                System.Diagnostics.Debug.WriteLine($"Error rendering KaTeX in preview: {ex.Message}");
            }
        }

        private string RenderHtml(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;
            
            // Sử dụng ExamRenderingService để normalize và render content
            return ExamRenderingService.NormalizeAndRenderContent(content, null, Exam?.OriginalExamPaperCore);
        }
        
        private char GetLetter(int order) => (char)('A' + Math.Max(0, order - 1));

        private List<OriginalExamPaperDetailDto> GetFlatQuestions()
        {
            if (Exam?.Details == null || Exam.Details.Count == 0)
                return new List<OriginalExamPaperDetailDto>();

            // Chỉ lấy các questions độc lập và parent questions (không bao gồm child questions)
            var filteredQuestions = new List<OriginalExamPaperDetailDto>();
            
            foreach (var q in Exam.Details.OrderBy(q => q.Order))
            {
                // Bỏ qua child questions (sẽ được render trong parent question)
                if (q.ParentQuestionId.HasValue)
                {
                    continue;
                }
                
                filteredQuestions.Add(q);
            }
            
            return filteredQuestions;
        }
        
        /// <summary>
        /// Lấy danh sách các parent questions không phải matching (câu hỏi nhóm)
        /// </summary>
        private List<OriginalExamPaperDetailDto> GetGroupParentQuestions()
        {
            if (Exam?.Details == null || Exam.Details.Count == 0)
                return new List<OriginalExamPaperDetailDto>();

            return Exam.Details
                .Where(q => q.ParentQuestionId == null 
                    && q.ChildQuestions != null 
                    && q.ChildQuestions.Count > 0 
                    && !IsMatchingParent(q))
                .OrderBy(q => q.Order)
                .ToList();
        }
        
        /// <summary>
        /// Trích xuất range từ nội dung group question (ví dụ: {<1>} — {<3>})
        /// </summary>
        private (int? start, int? end) ExtractGroupRange(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return (null, null);
            
            var match = System.Text.RegularExpressions.Regex.Match(content, @"\{<(\d+)>\}.*?\{<(\d+)>\}");
            if (match.Success && match.Groups.Count >= 3)
            {
                if (int.TryParse(match.Groups[1].Value, out int start) && 
                    int.TryParse(match.Groups[2].Value, out int end))
                {
                    return (start, end);
                }
            }
            
            return (null, null);
        }
        
        /// <summary>
        /// Lấy số thứ tự của parent question (group question được đếm như 1 câu hỏi)
        /// </summary>
        private int? GetGroupParentNumber(OriginalExamPaperDetailDto parent)
        {
            if (Exam?.Details == null || Exam.Details.Count == 0)
                return null;
            
            var allQuestions = Exam.Details.OrderBy(q => q.Order).ToList();
            int number = 1;
            
            foreach (var q in allQuestions)
            {
                if (q.OriginalExamPaperDetailId == parent.OriginalExamPaperDetailId)
                {
                    return number;
                }
                
                var isMatching = IsMatchingParent(q);
                var isGroupParent = q.ParentQuestionId == null && q.ChildQuestions != null && q.ChildQuestions.Count > 0 && !isMatching;
                
                if (isMatching)
                {
                    // Matching question: đếm như 1 câu hỏi
                    number++;
                }
                else if (isGroupParent)
                {
                    // Group question: parent đếm như 1 câu hỏi, child questions đếm tiếp
                    number++; // Đếm parent
                    if (q.ChildQuestions != null)
                    {
                        number += q.ChildQuestions.Count; // Đếm các child
                    }
                }
                else
                {
                    // Câu hỏi độc lập: đếm
                    number++;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Lấy số thứ tự đầu và cuối của child questions trong một nhóm
        /// Parent được đếm như 1 câu hỏi, child questions đếm tiếp sau parent
        /// </summary>
        private (int start, int end) GetChildQuestionsRange(OriginalExamPaperDetailDto parent)
        {
            if (parent.ChildQuestions == null || parent.ChildQuestions.Count == 0)
                return (0, 0);
            
            // Lấy số thứ tự của parent
            var parentNumber = GetGroupParentNumber(parent);
            if (!parentNumber.HasValue)
                return (0, 0);
            
            // Child questions bắt đầu từ parentNumber + 1
            int startNumber = parentNumber.Value + 1;
            int endNumber = startNumber + parent.ChildQuestions.Count - 1;
            
            return (startNumber, endNumber);
        }

        /// <summary>
        /// Kiểm tra xem đây có phải là group question (câu hỏi nhóm) không
        /// Group question có pattern {<1>} — {<3>} trong nội dung
        /// </summary>
        private bool IsGroupQuestion(OriginalExamPaperDetailDto q)
        {
            if (q.ChildQuestions == null || q.ChildQuestions.Count == 0)
                return false;

            // Kiểm tra pattern {<...>} trong nội dung parent question
            // Pattern có thể là: {<1>} — {<3>} hoặc {<1>} - {<3>} hoặc {<1>}—{<3>}
            var stem = q.QuestionContent ?? string.Empty;
            
            // Kiểm tra nhiều pattern khác nhau cho group question
            var patterns = new[]
            {
                @"\{<\d+>\}.*?\{<\d+>\}",  // {<1>} ... {<3>}
                @"\{&lt;\d+&gt;\}.*?\{&lt;\d+&gt;\}",  // HTML encoded: {&lt;1&gt;} ... {&lt;3&gt;}
                @"\{&lt;\d+&gt;\}.*?—.*?\{&lt;\d+&gt;\}",  // HTML encoded với dấu gạch ngang
            };
            
            foreach (var pattern in patterns)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(stem, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Kiểm tra xem đây có phải là matching question dạng parent-child không
        /// Matching question có đặc điểm:
        /// 1. Parent question không có answers (chỉ có stem)
        /// 2. Tất cả child questions có cùng số lượng answers
        /// 3. Parent question có từ khóa về matching (Nối cột, nối, match)
        /// 4. KHÔNG có pattern {<...>} (đó là group question)
        /// </summary>
        private bool IsMatchingParent(OriginalExamPaperDetailDto q)
        {
            if (q.ChildQuestions == null || q.ChildQuestions.Count == 0)
                return false;

            // QUAN TRỌNG: Kiểm tra group question TRƯỚC - nếu có pattern {<...>} thì chắc chắn không phải matching
            if (IsGroupQuestion(q))
                return false;

            // Parent question không nên có answers (chỉ có stem)
            if (q.Answers != null && q.Answers.Count > 0)
                return false;

            // Kiểm tra xem tất cả child questions có cùng số lượng answers không
            var firstChild = q.ChildQuestions.FirstOrDefault();
            if (firstChild == null || firstChild.Answers == null || firstChild.Answers.Count == 0)
                return false;

            var answerCount = firstChild.Answers.Count;
            
            // Tất cả child questions phải có cùng số lượng answers
            if (q.ChildQuestions.Any(child => child.Answers == null || child.Answers.Count != answerCount))
                return false;

            // Kiểm tra thêm: parent question có stem chứa từ khóa về matching không
            var stem = q.QuestionContent ?? string.Empty;
            var hasMatchingKeywords = stem.Contains("Nối cột", StringComparison.OrdinalIgnoreCase) ||
                                     stem.Contains("nối", StringComparison.OrdinalIgnoreCase) ||
                                     stem.Contains("match", StringComparison.OrdinalIgnoreCase);

            // CHỈ trả về true nếu CÓ từ khóa matching VÀ đáp ứng các điều kiện trên
            // Không dựa vào pattern (số lượng child >= 2 và số lượng answers >= 2) vì group questions cũng có thể có pattern này
            if (hasMatchingKeywords)
            {
                // Đảm bảo tất cả child questions đều là MCQ (có answers)
                return q.ChildQuestions.All(child => 
                    child.Answers != null && 
                    child.Answers.Count > 0 &&
                    child.Answers.Count == answerCount);
            }

            return false;
        }

        /// <summary>
        /// Lấy left items cho matching question (từ child questions)
        /// </summary>
        private List<AnswerStructureDto> GetMatchingLeftItems(OriginalExamPaperDetailDto parent)
        {
            var leftItems = new List<AnswerStructureDto>();
            
            if (parent.ChildQuestions == null)
                return leftItems;

            foreach (var child in parent.ChildQuestions.OrderBy(c => c.Order))
                    {
                // Tạo AnswerStructureDto từ child question stem
                leftItems.Add(new AnswerStructureDto
                {
                    AnswerId = child.OriginalExamPaperDetailId, // Dùng question ID làm AnswerId
                    AnswerContent = child.QuestionContent ?? string.Empty,
                    Order = child.Order,
                    OriginalExamPaperDetailId = child.OriginalExamPaperDetailId
                });
                    }

            return leftItems;
        }

        /// <summary>
        /// Lấy right items cho matching question (từ answers của child đầu tiên)
        /// </summary>
        private List<AnswerStructureDto> GetMatchingRightItems(OriginalExamPaperDetailDto parent)
        {
            if (parent.ChildQuestions == null || parent.ChildQuestions.Count == 0)
                return new List<AnswerStructureDto>();

            var firstChild = parent.ChildQuestions.OrderBy(c => c.Order).First();
            if (firstChild.Answers == null)
                return new List<AnswerStructureDto>();

            // Trả về answers từ child đầu tiên (tất cả child đều có cùng answers)
            return firstChild.Answers
                .OrderBy(a => a.Order)
                .Select(a => new AnswerStructureDto
                {
                    AnswerId = a.AnswerId,
                    AnswerContent = a.AnswerContent,
                    Order = a.Order,
                    OriginalExamPaperDetailId = a.OriginalExamPaperDetailId
                })
                .ToList();
        }

        /// <summary>
        /// Lấy đáp án đúng cho matching question (Dictionary<leftQuestionId, rightAnswerId>)
        /// </summary>
        private Dictionary<int, int> GetMatchingCorrectPairs(OriginalExamPaperDetailDto parent)
        {
            var correctPairs = new Dictionary<int, int>();
            
            if (parent.ChildQuestions == null || parent.ChildQuestions.Count == 0)
                return correctPairs;

            var firstChild = parent.ChildQuestions.OrderBy(c => c.Order).First();
            if (firstChild.Answers == null || firstChild.Answers.Count == 0)
                return correctPairs;

            // Lấy danh sách answers từ child đầu tiên (tất cả child đều có cùng answers)
            var rightAnswers = firstChild.Answers.OrderBy(a => a.Order).ToList();

            // Duyệt qua từng child question để lấy đáp án đúng
            foreach (var child in parent.ChildQuestions.OrderBy(c => c.Order))
            {
                if (child.CorrectAnswerIndex.HasValue && child.CorrectAnswerIndex.Value > 0)
                {
                    // CorrectAnswerIndex là 1-based, chuyển sang 0-based
                    var answerIndex = child.CorrectAnswerIndex.Value - 1;
                    
                    if (answerIndex >= 0 && answerIndex < rightAnswers.Count)
                    {
                        var correctAnswer = rightAnswers[answerIndex];
                        // leftQuestionId = child.OriginalExamPaperDetailId, rightAnswerId = correctAnswer.AnswerId
                        correctPairs[child.OriginalExamPaperDetailId] = correctAnswer.AnswerId;
                    }
                }
            }

            return correctPairs;
        }

        private int? GetQuestionNumber(OriginalExamPaperDetailDto question, int indexInFlatList)
        {
            if (Exam?.Details == null || Exam.Details.Count == 0)
                return null;
            
            // Đếm số thứ tự dựa trên thứ tự gốc trong Exam.Details (theo Order)
            // Sắp xếp tất cả questions theo Order
            var allQuestions = Exam.Details.OrderBy(q => q.Order).ToList();
            
            int number = 1;
            foreach (var q in allQuestions)
            {
                // Nếu đã đến câu hỏi hiện tại, dừng lại
                if (q.OriginalExamPaperDetailId == question.OriginalExamPaperDetailId)
                {
                    // Nếu là matching parent, trả về số đã đếm
                    if (IsMatchingParent(question))
                    {
                        return number;
                    }
                    // Nếu là parent question thông thường, không có số
                    if (question.ParentQuestionId == null && question.ChildQuestions != null && question.ChildQuestions.Count > 0)
                    {
                        return null;
                    }
                    // Câu hỏi độc lập: trả về số đã đếm
                    return number;
                }
                
                // Đếm các câu hỏi trước câu hỏi hiện tại
                var isMatching = IsMatchingParent(q);
                var isGroupParent = q.ParentQuestionId == null && q.ChildQuestions != null && q.ChildQuestions.Count > 0 && !isMatching;
                
                if (isMatching)
                {
                    // Matching question: đếm như một câu hỏi độc lập
                    number++;
                }
                else if (isGroupParent)
                {
                    // Group question: parent đếm như 1 câu hỏi, child questions đếm tiếp
                    number++; // Đếm parent
                    if (q.ChildQuestions != null)
                    {
                        number += q.ChildQuestions.Count; // Đếm các child
                    }
                }
                else
                {
                    // Câu hỏi độc lập: đếm
                    number++;
                }
            }
            
            // Nếu không tìm thấy trong allQuestions, trả về null
            return null;
        }
        
        /// <summary>
        /// Lấy số thứ tự của child question trong nhóm
        /// Parent được đếm như 1 câu hỏi, child questions đếm tiếp sau parent
        /// </summary>
        private int? GetChildQuestionNumber(OriginalExamPaperDetailDto childQuestion, OriginalExamPaperDetailDto parent)
        {
            if (parent.ChildQuestions == null || parent.ChildQuestions.Count == 0)
                return null;
            
            // Lấy số thứ tự của parent
            var parentNumber = GetGroupParentNumber(parent);
            if (!parentNumber.HasValue)
                return null;
            
            // Tìm thứ tự của child trong parent
            var orderedChildren = parent.ChildQuestions.OrderBy(c => c.Order).ToList();
            int childIndex = orderedChildren.FindIndex(c => c.OriginalExamPaperDetailId == childQuestion.OriginalExamPaperDetailId);
            
            if (childIndex == -1)
                return null;
            
            // Child questions bắt đầu từ parentNumber + 1
            return parentNumber.Value + 1 + childIndex;
        }

        private int GetTotalQuestionsCount()
        {
            if (Exam?.Details == null || Exam.Details.Count == 0)
                return 0;

            // Sử dụng ExamRenderingService để đếm tổng số câu hỏi
            return ExamRenderingService.GetTotalQuestionsCount(
                Exam.Details,
                q => q.ParentQuestionId,
                q => q.ChildQuestions
            );
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
                        StateHasChanged(); // Trigger re-render
                        // KaTeX sẽ được render trong OnAfterRenderAsync
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
                        StateHasChanged(); // Trigger re-render
                        // KaTeX sẽ được render trong OnAfterRenderAsync
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

