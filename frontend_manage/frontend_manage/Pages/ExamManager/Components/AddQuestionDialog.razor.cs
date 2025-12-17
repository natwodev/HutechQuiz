using frontend_manage.DTOs;
using frontend_manage.Services.ExamManager;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Linq;

namespace frontend_manage.Pages.ExamManager.Components
{
    public partial class AddQuestionDialog : ComponentBase
    {
        [CascadingParameter] 
        IMudDialogInstance MudDialog { get; set; } = default!;
        
        [Parameter] 
        public CreateQuestionWithAnswersRequest QuestionRequest { get; set; } = new();
        
        [Inject] 
        private ExamManagerService ExamManagerService { get; set; } = null!;
        
        [Inject] 
        private ISnackbar Snackbar { get; set; } = null!;

        private CreateQuestionWithAnswersRequest questionRequest = new();
        private bool isSubmitting = false;
        private string? message;

        protected override void OnInitialized()
        {
            // Tạo bản sao để tránh thay đổi object gốc
            questionRequest = new CreateQuestionWithAnswersRequest
            {
                OriginalExamPaperId = QuestionRequest.OriginalExamPaperId,
                Order = QuestionRequest.Order,
                QuestionContent = QuestionRequest.QuestionContent,
                CorrectAnswerIndex = QuestionRequest.CorrectAnswerIndex,
                ParentQuestionId = QuestionRequest.ParentQuestionId,
                ChapterId = QuestionRequest.ChapterId,
                CanShuffleQuestion = QuestionRequest.CanShuffleQuestion,
                Answers = QuestionRequest.Answers.Select(a => new CreateAnswerRequest
                {
                    Order = a.Order,
                    AnswerContent = a.AnswerContent,
                    IsCorrect = a.IsCorrect,
                    CanShuffleAnswer = a.CanShuffleAnswer
                }).ToList()
            };
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
            }
        }

        private async Task HandleSubmit()
        {
            // Đảm bảo Order của các đáp án được set đúng
            for (int i = 0; i < questionRequest.Answers.Count; i++)
            {
                questionRequest.Answers[i].Order = i + 1;
            }

            isSubmitting = true;
            message = null;
            StateHasChanged();

            try
            {
                var result = await ExamManagerService.AddQuestionWithAnswersAsync(questionRequest);
                
                if (result != null)
                {
                    Snackbar.Add("Thêm câu hỏi thành công!", Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    message = "Lỗi: Không thể thêm câu hỏi";
                }
            }
            catch (Exception ex)
            {
                message = $"Lỗi: {ex.Message}";
            }
            finally
            {
                isSubmitting = false;
                StateHasChanged();
            }
        }

        private void Cancel() => MudDialog.Cancel();
    }
}

