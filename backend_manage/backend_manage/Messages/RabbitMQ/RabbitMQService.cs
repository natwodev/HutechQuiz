using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using backend_manage.Data;
using Microsoft.Extensions.DependencyInjection;
using backend_manage.Messages;
using backend_manage.Services;

namespace backend_manage.Messages.RabbitMQ;

public class RabbitMqService : IRabbitMqService, IDisposable
{
    private IConnection? _connection;
    private IModel? _channel;
    private readonly ILogger<RabbitMqService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMessageProcessingService _messageProcessingService;

    public bool IsConnected => _connection != null && _connection.IsOpen && _channel != null && _channel.IsOpen;

    public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger, IMessageProcessingService messageProcessingService)
    {
        _logger = logger;
        _configuration = configuration;
        _messageProcessingService = messageProcessingService;
        TryInitializeConnection(); // ✅ dùng try-catch
    }

    private void TryInitializeConnection()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"],
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"],
                Port = _configuration.GetValue<int>("RabbitMQ:Port", 5672),
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _logger.LogInformation("✅ Đã kết nối RabbitMQ thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Không thể kết nối RabbitMQ.");
            _connection = null;
            _channel = null;
        }
    }

    public void Publish<T>(string queueName, T message)
    {
        try
        {
            if (!IsConnected)
            {
                _logger.LogWarning("⚠️ RabbitMQ chưa kết nối, thực hiện trực tiếp và lưu vào database.");
                _messageProcessingService.ProcessMessageAsync(queueName, message).Wait();
                return;
            }

            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);

            var props = _channel.CreateBasicProperties();
            props.Persistent = true;
            props.ContentType = "application/json";

            _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: props, body: body);
            _logger.LogInformation("📤 Đã gửi message tới queue {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi gửi message tới queue {QueueName}, thực hiện trực tiếp và lưu vào database.", queueName);
            _messageProcessingService.ProcessMessageAsync(queueName, message).Wait();
        }
    }

    public void Subscribe<T>(string queueName, Func<T, Task> onMessage)
    {
        try
        {
            if (_connection == null || !_connection.IsOpen)
            {
                _logger.LogWarning("⚠️ Không thể đăng ký consumer vì chưa kết nối RabbitMQ.");
                return;
            }

            var (prefetchCount, batchSize, timerInterval) = GetQueueConfig(queueName);

            var channel = _connection.CreateModel(); // ✅ tạo channel riêng
            channel.BasicQos(0, prefetchCount, false);
            channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (sender, args) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(args.Body.ToArray());
                    var message = JsonConvert.DeserializeObject<T>(json);

                    await onMessage(message);

                    channel.BasicAck(args.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Lỗi xử lý message từ queue {QueueName}", queueName);
                    channel.BasicNack(args.DeliveryTag, false, true); // requeue
                }
            };

            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("📥 Đã đăng ký consumer cho queue {QueueName} (Prefetch={PrefetchCount})", queueName, prefetchCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi đăng ký consumer cho queue {QueueName}", queueName);
        }
    }

    private (ushort prefetchCount, int batchSize, TimeSpan timerInterval) GetQueueConfig(string queueName)
    {
        return queueName switch
        {
            "start_exam_queue" => (50, 1, TimeSpan.FromMilliseconds(100)),
            "exam_submission_queue" => (30, 1, TimeSpan.FromMilliseconds(150)),
            "save_answer_queue" => (100, 1, TimeSpan.FromMilliseconds(50)),
            _ => (75, 1, TimeSpan.FromMilliseconds(50))
        };
    }

    public void CheckQueueStatus(string queueName)
    {
        try
        {
            if (_channel == null || !_channel.IsOpen)
            {
                _logger.LogWarning("⚠️ Không thể kiểm tra trạng thái queue vì chưa kết nối RabbitMQ.");
                return;
            }

            var result = _channel.QueueDeclarePassive(queueName);
            _logger.LogInformation("📊 Queue {QueueName} có {MessageCount} messages, {ConsumerCount} consumers",
                queueName, result.MessageCount, result.ConsumerCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Không thể kiểm tra trạng thái queue {QueueName}", queueName);
        }
    }

    public void TryReconnect()
    {
        if (!IsConnected)
        {
            Dispose();
            _logger.LogWarning("🔁 Đang thử reconnect RabbitMQ...");
            TryInitializeConnection();
        }
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
            _channel?.Dispose();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi khi đóng kết nối RabbitMQ");
        }
    }
}
