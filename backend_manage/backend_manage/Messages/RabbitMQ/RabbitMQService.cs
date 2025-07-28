using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace backend_manage.Messages.RabbitMQ
{
    public class RabbitMqService : IRabbitMqService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqService> _logger;
        private readonly Dictionary<string, object> _consumers;

        public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
        {
            _logger = logger;
            _consumers = new Dictionary<string, object>();

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
                    UserName = configuration["RabbitMQ:UserName"] ?? "guest",
                    Password = configuration["RabbitMQ:Password"] ?? "guest",
                    Port = configuration["RabbitMQ:Port"] != null ? int.Parse(configuration["RabbitMQ:Port"]) : 5672
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declare tất cả queues một lần khi khởi tạo
                DeclareQueues();

                _logger.LogInformation("Kết nối RabbitMQ thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể kết nối tới RabbitMQ");
                throw;
            }
        }

        public void PublishMessage<T>(string queueName, T message)
        {
            try
            {
                var json = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";

                _channel.BasicPublish(
                    exchange: string.Empty,
                    routingKey: queueName,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation("Đã gửi message tới queue {QueueName}: {Message}", queueName, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi message tới queue {QueueName}", queueName);
                throw;
            }
        }

        public void Subscribe<T>(string queueName, Action<T> onMessage)
        {
            try
            {
                // Cấu hình riêng cho từng loại queue
                var (prefetchCount, batchSize, timerInterval) = GetQueueConfig(queueName);
                _channel.BasicQos(0, prefetchCount, false);

                var consumer = new EventingBasicConsumer(_channel);
                var messageBatch = new List<(T message, ulong deliveryTag)>();
                var batchLock = new object();
                var batchTimer = new Timer(_ => ProcessBatch(), null, timerInterval, timerInterval);

                void ProcessBatch()
                {
                    lock (batchLock)
                    {
                        if (messageBatch.Count > 0)
                        {
                            try
                            {
                                var batchToProcess = new List<(T message, ulong deliveryTag)>(messageBatch);
                                messageBatch.Clear();

                                foreach (var (message, deliveryTag) in batchToProcess)
                                {
                                    try
                                    {
                                        onMessage(message);
                                        if (_channel.IsOpen)
                                        {
                                            _channel.BasicAck(deliveryTag, false);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Lỗi khi xử lý message với delivery tag {DeliveryTag}", deliveryTag);
                                        if (_channel.IsOpen)
                                        {
                                            _channel.BasicNack(deliveryTag, false, true);
                                        }
                                    }
                                }
                                _logger.LogInformation("Đã xử lý batch {Count} messages từ queue {QueueName}", batchToProcess.Count, queueName);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Lỗi khi xử lý batch messages từ queue {QueueName}", queueName);
                            }
                        }
                    }
                }

                consumer.Received += (sender, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var data = JsonConvert.DeserializeObject<T>(message);

                        lock (batchLock)
                        {
                            messageBatch.Add((data, ea.DeliveryTag));

                            // Xử lý ngay nếu đủ batch size
                            if (messageBatch.Count >= batchSize)
                            {
                                ProcessBatch();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi nhận message từ queue {QueueName}", queueName);
                        if (_channel.IsOpen)
                        {
                            _channel.BasicNack(ea.DeliveryTag, false, true);
                        }
                    }
                };

                var consumerTag = _channel.BasicConsume(
                    queue: queueName,
                    autoAck: false,
                    consumer: consumer
                );

                _consumers[queueName] = consumer;

                _logger.LogInformation("Bắt đầu nhận message từ queue: {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng ký nhận message từ queue {QueueName}", queueName);
                throw;
            }
        }

        private void DeclareQueues()
        {
            var queues = new[]
            {
                "student_answer_saved_queue",
                "exam_submission_queue"
            };

            foreach (var queueName in queues)
            {
                _channel.QueueDeclare(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );
                _logger.LogInformation("Đã declare queue: {QueueName}", queueName);
            }
        }

        private (ushort prefetchCount, int batchSize, TimeSpan timerInterval) GetQueueConfig(string queueName)
        {
            return queueName switch
            {
                "student_answer_saved_queue" => (20, 10, TimeSpan.FromSeconds(1)), // High throughput cho lưu đáp án
                "exam_submission_queue" => (10, 5, TimeSpan.FromMilliseconds(500)), // Low latency cho nộp bài
                _ => (15, 8, TimeSpan.FromSeconds(1)) // Default config
            };
        }

        public void Dispose()
        {
            try
            {
                if (_channel?.IsOpen ?? false)
                {
                    _channel.Close();
                }
                if (_connection?.IsOpen ?? false)
                {
                    _connection.Close();
                }
                _channel?.Dispose();
                _connection?.Dispose();

                _logger.LogInformation("Đã đóng và giải phóng kết nối RabbitMQ");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đóng kết nối RabbitMQ");
            }
        }
    }
} 