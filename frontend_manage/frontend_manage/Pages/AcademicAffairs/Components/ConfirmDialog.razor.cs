using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace frontend_manage.Pages.AcademicAffairs.Components
{
    public partial class ConfirmDialog : BaseComponent
    {
        [CascadingParameter] 
        public dynamic MudDialog { get; set; } = default!;

        [Parameter] 
        public string ContentText { get; set; } = string.Empty;

        [Parameter] 
        public string ButtonText { get; set; } = "Xác nhận";

        [Parameter] 
        public Color Color { get; set; } = Color.Primary;

        private void Cancel()
        {
            MudDialog.Cancel();
        }

        private void Submit()
        {
            MudDialog.Close(DialogResult.Ok(true));
        }
    }
}
