using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Net.Http;
using frontend_manage.Services;

namespace frontend_manage
{
    public class BaseComponent : ComponentBase
    {
        [Inject]
        protected InjectedServices Services { get; set; }

        // Convenience properties for easy access
        protected NavigationManager Navigation => Services.Navigation;
        protected StudentService StudentService => Services.StudentService;
        protected ISnackbar Snackbar => Services.Snackbar;
        protected IDialogService Dialog => Services.Dialog;
        protected HttpClient Http => Services.Http;
        protected IJSRuntime JSRuntime => Services.JSRuntime;
        protected IMathJaxService MathJaxService => Services.MathJaxService;
    }
}
