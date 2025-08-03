using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace backend_manage.Messages.RabbitMQ.FallBack;

public class FallbackRetryWorker : BackgroundService
{
    private readonly IRabbitMqService _rabbitMqService;
    private readonly ILogger<FallbackRetryWorker> _logger;

    public FallbackRetryWorker(IRabbitMqService rabbitMqService, ILogger<FallbackRetryWorker> logger)
    {
        _rabbitMqService = rabbitMqService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔁 FallbackRetryWorker đang chạy...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_rabbitMqService.IsConnected && _rabbitMqService.HasFallbackMessages)
                {
                    var message = await _rabbitMqService.DequeueFallbackMessageAsync(stoppingToken);

                    if (message is not null)
                    {
                        _rabbitMqService.Publish(message.QueueName, message.Message);
                        _logger.LogInformation("🔁 Đã retry gửi message từ fallback queue vào RabbitMQ");
                    }
                }
                else
                {
                    await Task.Delay(500, stoppingToken); // ngủ ngắn nếu không có việc
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi trong FallbackRetryWorker");
                await Task.Delay(1000, stoppingToken); // ngủ lâu hơn nếu lỗi
            }
        }
    }
}