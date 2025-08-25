using Microsoft.JSInterop;

namespace frontend_manage.Services
{
    public interface IMathJaxService
    {
        Task<string> ProcessLatexAsync(string latex, bool isDisplay = false);
        Task TypesetAsync();
        Task ClearTypesetAsync();
    }

    public class MathJaxService : IMathJaxService
    {
        private readonly IJSRuntime _jsRuntime;

        public MathJaxService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<string> ProcessLatexAsync(string latex, bool isDisplay = false)
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string>("mathJaxBlazor.processLatex", latex, isDisplay);
            }
            catch (Exception ex)
            {
                // Log error and return original latex if MathJax fails
                Console.WriteLine($"Error processing LaTeX: {ex.Message}");
                return latex;
            }
        }

        public async Task TypesetAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("mathJaxBlazor.typesetPromise");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error typesetting: {ex.Message}");
            }
        }

        public async Task ClearTypesetAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("mathJaxBlazor.typesetClear");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing typeset: {ex.Message}");
            }
            }
    }
}
