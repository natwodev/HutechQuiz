using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace backend_manage.core.Messages.RabbitMQ;

public class RabbitMqReconnectWorker : BackgroundService
{
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ILogger<RabbitMqReconnectWorker> _logger;
    private readonly TimeSpan _retryInterval = TimeSpan.FromSeconds(10);

    public RabbitMqReconnectWorker(IRabbitMqService rabbitMqService, ILogger<RabbitMqReconnectWorker> logger)
    {
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_rabbitMqService.IsConnected)
                {
                    _logger.LogInformation("🔄 Đang thử reconnect RabbitMQ...");
                    _rabbitMqService.TryReconnect();
                    
                    if (_rabbitMqService.IsConnected)
                    {
                        _logger.LogInformation("✅ Đã reconnect RabbitMQ thành công");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Không thể reconnect RabbitMQ, sẽ thử lại sau {Interval} giây", _retryInterval.TotalSeconds);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi thử reconnect RabbitMQ.");
            }

            await Task.Delay(_retryInterval, stoppingToken);
        }
    }
}