using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace frontend_manage.Services
{
    public interface IKaTeXService
    {
        /// <summary>
        /// Render KaTeX cho tất cả elements khớp với selector
        /// </summary>
        ValueTask RenderAsync(string elementSelector);

        /// <summary>
        /// Render KaTeX cho một element cụ thể
        /// </summary>
        ValueTask RenderElementAsync(ElementReferenceWrapper elementRef);

        /// <summary>
        /// Kiểm tra xem KaTeX đã sẵn sàng chưa
        /// </summary>
        ValueTask<bool> IsKaTeXReadyAsync();

        /// <summary>
        /// Render với retry logic nếu KaTeX chưa sẵn sàng
        /// </summary>
        ValueTask RenderWithRetryAsync(string elementSelector, int maxRetries = 3, int delayMs = 100);
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
        private readonly ILogger<KaTeXService>? _logger;

        public KaTeXService(IJSRuntime jsRuntime, ILogger<KaTeXService>? logger = null)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        public ValueTask RenderAsync(string elementSelector)
        {
            if (string.IsNullOrWhiteSpace(elementSelector))
            {
                _logger?.LogWarning("KaTeXService.RenderAsync called with empty selector");
                return ValueTask.CompletedTask;
            }

            try
            {
                return _jsRuntime.InvokeVoidAsync("katexInterop.renderAllInSelector", elementSelector);
            }
            catch (JSException ex)
            {
                _logger?.LogError(ex, "Error rendering KaTeX with selector: {Selector}", elementSelector);
                throw;
            }
        }

        public ValueTask RenderElementAsync(ElementReferenceWrapper elementRef)
        {
            if (elementRef.Reference == null)
            {
                _logger?.LogWarning("KaTeXService.RenderElementAsync called with null element reference");
                return ValueTask.CompletedTask;
            }

            try
            {
                return _jsRuntime.InvokeVoidAsync("katexInterop.renderAllInElement", elementRef.Reference);
            }
            catch (JSException ex)
            {
                _logger?.LogError(ex, "Error rendering KaTeX for element reference");
                throw;
            }
        }

        public async ValueTask<bool> IsKaTeXReadyAsync()
        {
            try
            {
                var isReady = await _jsRuntime.InvokeAsync<bool>("eval", "typeof window.renderMathInElement !== 'undefined' && typeof window.katexInterop !== 'undefined'");
                return isReady;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error checking KaTeX readiness");
                return false;
            }
        }

        public async ValueTask RenderWithRetryAsync(string elementSelector, int maxRetries = 3, int delayMs = 100)
        {
            if (string.IsNullOrWhiteSpace(elementSelector))
            {
                _logger?.LogWarning("KaTeXService.RenderWithRetryAsync called with empty selector");
                return;
            }

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    // Kiểm tra xem KaTeX đã sẵn sàng chưa
                    var isReady = await IsKaTeXReadyAsync();
                    if (!isReady)
                    {
                        _logger?.LogDebug("KaTeX not ready, waiting {DelayMs}ms before retry {Retry}/{MaxRetries}", delayMs, i + 1, maxRetries);
                        await Task.Delay(delayMs);
                        continue;
                    }

                    // Render KaTeX
                    await RenderAsync(elementSelector);
                    _logger?.LogDebug("KaTeX rendered successfully for selector: {Selector}", elementSelector);
                    return;
                }
                catch (JSException ex)
                {
                    _logger?.LogWarning(ex, "Error rendering KaTeX (attempt {Retry}/{MaxRetries})", i + 1, maxRetries);
                    
                    if (i == maxRetries - 1)
                    {
                        _logger?.LogError(ex, "Failed to render KaTeX after {MaxRetries} attempts", maxRetries);
                        throw;
                    }

                    await Task.Delay(delayMs);
                }
            }
        }
    }
}


