using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace frontend_manage.Services
{
    public interface IKaTeXService
    {
        ValueTask RenderAsync(string elementSelector);
        ValueTask RenderElementAsync(ElementReferenceWrapper elementRef);
    }

    // Wrapper to avoid direct ElementReference dependency in non-Razor file
    public readonly struct ElementReferenceWrapper
    {
        public ElementReferenceWrapper(object reference)
        {
            Reference = reference;
        }

        public object Reference { get; }
    }

    public class KaTeXService : IKaTeXService
    {
        private readonly IJSRuntime _jsRuntime;

        public KaTeXService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public ValueTask RenderAsync(string elementSelector)
        {
            return _jsRuntime.InvokeVoidAsync("katexInterop.renderAllInSelector", elementSelector);
        }

        public ValueTask RenderElementAsync(ElementReferenceWrapper elementRef)
        {
            return _jsRuntime.InvokeVoidAsync("katexInterop.renderAllInElement", elementRef.Reference);
        }
    }
}


