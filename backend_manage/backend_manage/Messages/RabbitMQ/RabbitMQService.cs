using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace backend_manage.Messages.RabbitMQ;

public class RabbitMqService : IRabbitMqService, IDisposable
{
    private IConnection _connection;
    private IModel _channel;
    private readonly ILogger<RabbitMqService> _logger;
    private readonly IConfiguration _configuration;

    public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
    {
        _logger = logger;
        _configuration = configuration;
        InitializeConnection();
    }

    private void InitializeConnection()
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
    }

    public void Publish<T>(string queueName, T message)
    {
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        var json = JsonConvert.SerializeObject(message);
        var body = Encoding.UTF8.GetBytes(json);

        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";

        _channel.BasicPublish(
            exchange: "",
            routingKey: queueName,
            basicProperties: props,
            body: body
        );

        _logger.LogInformation("📤 Đã gửi message tới queue {QueueName}", queueName);
    }

    public void Subscribe<T>(string queueName, Func<T, Task> onMessage)
    {
        var (prefetchCount, batchSize, timerInterval) = GetQueueConfig(queueName);

        var channel = _connection.CreateModel(); // ✅ tạo channel riêng
        channel.BasicQos(0, prefetchCount, false); // ✅ giới hạn số message đang xử lý đồng thời

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

    private (ushort prefetchCount, int batchSize, TimeSpan timerInterval) GetQueueConfig(string queueName)
    {
        return queueName switch
        {
            "start_exam_queue"     => (50,  1, TimeSpan.FromMilliseconds(100)),
            _                      => (75,  1, TimeSpan.FromMilliseconds(50))
        };
    }


    public void CheckQueueStatus(string queueName)
    {
        var result = _channel.QueueDeclarePassive(queueName);
        _logger.LogInformation("📊 Queue {QueueName} có {MessageCount} messages, {ConsumerCount} consumers",
            queueName, result.MessageCount, result.ConsumerCount);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}