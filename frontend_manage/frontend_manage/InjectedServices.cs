using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Net.Http;
using frontend_manage.Services;

namespace frontend_manage
{
    public class InjectedServices
    {
        public NavigationManager Navigation { get; set; }
        public StudentService StudentService { get; set; }
        public ISnackbar Snackbar { get; set; }
        public IDialogService Dialog { get; set; }
        public HttpClient Http { get; set; }
        public IJSRuntime JSRuntime { get; set; }
        public IMathJaxService MathJaxService { get; set; }
    }
}
