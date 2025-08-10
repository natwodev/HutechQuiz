using Microsoft.AspNetCore.Components;
using MudBlazor;
using frontend_manage.DTOs.AcademicAffairs;

namespace frontend_manage.Pages.AcademicAffairs.Components
{
    public partial class ExamBatchDialog : ComponentBase
    {
        [CascadingParameter] 
        public dynamic MudDialog { get; set; } = default!;

        [Parameter] 
        public string Title { get; set; } = string.Empty;

        [Parameter] 
        public string ButtonText { get; set; } = "Lưu";

        [Parameter] 
        public Color Color { get; set; } = Color.Primary;

        [Parameter] 
        public ExamBatchUpdateDto? ExamBatch { get; set; }

        [Parameter] 
        public List<SemesterDto> Semesters { get; set; } = new();

        private ExamBatchUpdateDto examBatch = new()
        {
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddDays(7)
        };

        protected override void OnInitialized()
        {
            if (ExamBatch != null)
            {
                examBatch = ExamBatch;
            }
        }

        private void Cancel()
        {
            MudDialog.Cancel();
        }

        private void Submit()
        {
            // Validate that end date is after start date
            if (examBatch.EndDate < examBatch.StartDate)
            {
                // Show error
                return;
            }

            MudDialog.Close(DialogResult.Ok(examBatch));
        }
    }
}
