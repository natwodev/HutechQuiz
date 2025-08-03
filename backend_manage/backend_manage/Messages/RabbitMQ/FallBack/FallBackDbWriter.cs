namespace backend_manage.Messages.RabbitMQ.FallBack;

public class FallBackDbWriter<T> : BackgroundService
{
    private readonly FallBack<T> _fallBackQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FallBackDbWriter<T>> _logger;
    private readonly Func<T, IServiceProvider, Task> _saveFunc;

    public FallBackDbWriter(
        FallBack<T> fallBackQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<FallBackDbWriter<T>> logger,
        Func<T, IServiceProvider, Task> saveFunc)
    {
        _fallBackQueue = fallBackQueue;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _saveFunc = saveFunc;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _fallBackQueue.DequeueAsync(stoppingToken);
                if (message != null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var services = scope.ServiceProvider;

                    await _saveFunc(message, services); // xử lý ghi vào DB
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi ghi message từ fallback queue vào DB.");
            }
        }
    }
}