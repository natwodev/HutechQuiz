using Microsoft.AspNetCore.Components;
using MudBlazor;
using frontend_manage.DTOs.AcademicAffairs;
using frontend_manage.Services.AcademicAffairs;

namespace frontend_manage.Pages.AcademicAffairs.Components
{
    public partial class ExamSessionDialog : BaseComponent
    {
        [Inject] 
        public ExamBatchDetailService ExamBatchDetailService { get; set; } = default!;

        [CascadingParameter] 
        public dynamic MudDialog { get; set; } = default!;

        [Parameter] 
        public string Title { get; set; } = string.Empty;

        [Parameter] 
        public string ButtonText { get; set; } = "Lưu";

        [Parameter] 
        public Color Color { get; set; } = Color.Primary;

        [Parameter] 
        public ExamSessionUpdateDto? ExamSession { get; set; }

        [Parameter] 
        public List<ExamBatchDto> ExamBatches { get; set; } = new();

        private ExamSessionUpdateDto examSession = new()
        {
            StartTime = DateTime.Now.AddHours(1),
            EndTime = DateTime.Now.AddHours(3)
        };

        private int selectedExamBatchId;
        private List<ExamBatchDetailDto> examBatchDetails = new();

        protected override async Task OnInitializedAsync()
        {
            if (ExamSession != null)
            {
                examSession = ExamSession;
                // Try to find the exam batch for the exam batch detail
                var examBatchDetail = await GetExamBatchDetailAsync(examSession.ExamBatchDetailId);
                if (examBatchDetail != null)
                {
                    selectedExamBatchId = examBatchDetail.ExamBatchId;
                    await LoadExamBatchDetails(selectedExamBatchId);
                }
            }
        }

        private async Task<ExamBatchDetailDto?> GetExamBatchDetailAsync(int id)
        {
            try
            {
                return await ExamBatchDetailService.GetByIdAsync(id);
            }
            catch
            {
                return null;
            }
        }

        private async Task LoadExamBatchDetails(int examBatchId)
        {
            try
            {
                var allDetails = await ExamBatchDetailService.GetAllAsync();
                examBatchDetails = allDetails.Where(d => d.ExamBatchId == examBatchId).ToList();
            }
            catch
            {
                examBatchDetails = new List<ExamBatchDetailDto>();
            }
        }

        private void Cancel()
        {
            MudDialog.Cancel();
        }

        private void Submit()
        {
            // Validate that end time is after start time
            if (examSession.EndTime <= examSession.StartTime)
            {
                // Show error
                return;
            }

            MudDialog.Close(DialogResult.Ok(examSession));
        }
    }
}
