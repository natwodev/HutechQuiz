using Microsoft.AspNetCore.Components;
using MudBlazor;
using frontend_manage.DTOs.AcademicAffairs;

namespace frontend_manage.Pages.AcademicAffairs.Components
{
    public partial class SemesterDialog : ComponentBase
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
        public SemesterUpdateDto? Semester { get; set; }

        [Parameter] 
        public List<AcademicYearDto> AcademicYears { get; set; } = new();

        private SemesterUpdateDto semester = new();

        protected override void OnInitialized()
        {
            if (Semester != null)
            {
                semester = Semester;
            }
        }

        private void Cancel()
        {
            MudDialog.Cancel();
        }

        private void Submit()
        {
            MudDialog.Close(DialogResult.Ok(semester));
        }
    }
}
