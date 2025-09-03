using Microsoft.AspNetCore.Components;
using MudBlazor;
using frontend_manage.DTOs.AcademicAffairs;

namespace frontend_manage.Pages.AcademicAffairs.Components
{
    public partial class AcademicYearDialog : BaseComponent
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
        public AcademicYearUpdateDto? AcademicYear { get; set; }

        private AcademicYearUpdateDto academicYear = new();

        protected override void OnInitialized()
        {
            if (AcademicYear != null)
            {
                academicYear = AcademicYear;
            }
        }

        private void Cancel()
        {
            MudDialog.Cancel();
        }

        private void Submit()
        {
            MudDialog.Close(DialogResult.Ok(academicYear));
        }
    }
}
