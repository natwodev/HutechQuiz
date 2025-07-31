using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading.Tasks;

namespace backend_manage.Messages.RabbitMQ
{
    public class RabbitMqFallbackService : IRabbitMqService
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, Func<object, Task>> _messageHandlers;

        public RabbitMqFallbackService(ILogger logger)
        {
            _logger = logger;
            _messageHandlers = new Dictionary<string, Func<object, Task>>();
            _logger.LogWarning("Sử dụng RabbitMQ fallback service - RabbitMQ không khả dụng, sẽ thực hiện tác vụ trực tiếp");
        }

        public void PublishMessage<T>(string queueName, T message)
        {
            _logger.LogWarning("RabbitMQ không khả dụng - thực hiện tác vụ trực tiếp cho queue {QueueName}", queueName);
            
            // Thực hiện tác vụ trực tiếp thay vì bỏ qua
            try
            {
                if (_messageHandlers.ContainsKey(queueName))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _messageHandlers[queueName](message);
                            _logger.LogInformation("Đã thực hiện tác vụ trực tiếp cho queue {QueueName}", queueName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi khi thực hiện tác vụ trực tiếp cho queue {QueueName}", queueName);
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("Không có handler cho queue {QueueName}, bỏ qua message", queueName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý message trực tiếp cho queue {QueueName}", queueName);
            }
        }

        public void Subscribe<T>(string queueName, Func<T, Task> onMessage)
        {
            _logger.LogInformation("Đăng ký handler trực tiếp cho queue {QueueName}", queueName);
            
            // Lưu handler để sử dụng khi publish message
            _messageHandlers[queueName] = async (message) =>
            {
                if (message is T typedMessage)
                {
                    await onMessage(typedMessage);
                }
                else
                {
                    _logger.LogWarning("Message type không khớp cho queue {QueueName}", queueName);
                }
            };
        }

        public void CheckQueueStatus(string queueName)
        {
            _logger.LogWarning("RabbitMQ không khả dụng - không thể kiểm tra trạng thái queue {QueueName}", queueName);
        }

        public void Dispose()
        {
            _logger.LogInformation("Dispose RabbitMQ fallback service");
        }
    }

    public class RabbitMqService : IRabbitMqService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqService> _logger;
        private readonly Dictionary<string, List<(EventingBasicConsumer Consumer, string ConsumerTag)>> _consumers;

        public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
        {
            _logger = logger;
            _consumers = new Dictionary<string, List<(EventingBasicConsumer Consumer, string ConsumerTag)>>();

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

        public void Subscribe<T>(string queueName, Func<T, Task> onMessage)
        {
            try
            {
                // Cấu hình riêng cho từng loại queue
                var (prefetchCount, batchSize, timerInterval) = GetQueueConfig(queueName);
                _channel.BasicQos(0, prefetchCount, false);

                var consumer = new EventingBasicConsumer(_channel);
                var messageBatch = new List<(T message, ulong deliveryTag)>();
                var batchLock = new object();
                var batchTimer = new Timer(async _ => await ProcessBatchAsync(), null, timerInterval, timerInterval);

                async Task ProcessBatchAsync()
                {
                    List<(T message, ulong deliveryTag)> batchToProcess;
                    
                    lock (batchLock)
                    {
                        if (messageBatch.Count == 0)
                        {
                            _logger.LogDebug("Không có message nào trong batch để xử lý từ queue {QueueName}", queueName);
                            return;
                        }

                        batchToProcess = new List<(T message, ulong deliveryTag)>(messageBatch);
                        messageBatch.Clear();
                        _logger.LogInformation("Bắt đầu xử lý batch {Count} messages từ queue {QueueName}", batchToProcess.Count, queueName);
                    }

                    // Xử lý async để không block RabbitMQ
                    foreach (var (message, deliveryTag) in batchToProcess)
                    {
                        try
                        {
                        _logger.LogDebug("Đang xử lý message với delivery tag {DeliveryTag}", deliveryTag);
                        
                        // Xử lý message async
                        await onMessage(message);
                        
                        if (_channel.IsOpen)
                        {
                            _channel.BasicAck(deliveryTag, false);
                            _logger.LogDebug("Đã xác nhận message với delivery tag {DeliveryTag}", deliveryTag);
                        }
                        else
                        {
                            _logger.LogWarning("Kênh đã đóng, không thể xác nhận message {DeliveryTag}", deliveryTag);
                        }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi khi xử lý message với delivery tag {DeliveryTag}", deliveryTag);
                            if (_channel.IsOpen)
                            {
                                _channel.BasicNack(deliveryTag, false, true);
                                _logger.LogDebug("Đã từ chối message với delivery tag {DeliveryTag}", deliveryTag);
                            }
                            else
                            {
                                _logger.LogWarning("Kênh đã đóng, không thể từ chối message {DeliveryTag}", deliveryTag);
                            }
                        }
                    }
                    
                    _logger.LogInformation("Hoàn thành xử lý batch {Count} messages từ queue {QueueName}", batchToProcess.Count, queueName);
                }

                consumer.Received += (sender, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var data = JsonConvert.DeserializeObject<T>(message);

                        _logger.LogDebug("Đã nhận message với delivery tag {DeliveryTag} từ queue {QueueName}", ea.DeliveryTag, queueName);

                        lock (batchLock)
                        {
                            messageBatch.Add((data, ea.DeliveryTag));
                            _logger.LogDebug("Đã thêm message vào batch. Kích thước batch hiện tại: {BatchSize}/{MaxBatchSize}", messageBatch.Count, batchSize);

                            // Xử lý ngay khi có message - Không chờ batch
                            if (messageBatch.Count > 0)
                            {
                                _logger.LogInformation("Có {Count} messages, xử lý ngay", messageBatch.Count);
                                _ = ProcessBatchAsync(); // Fire-and-forget async
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi nhận message từ queue {QueueName}", queueName);
                        if (_channel.IsOpen)
                        {
                            _channel.BasicNack(ea.DeliveryTag, false, true);
                            _logger.LogDebug("Đã từ chối message với delivery tag {DeliveryTag} do lỗi", ea.DeliveryTag);
                        }
                        else
                        {
                            _logger.LogWarning("Kênh đã đóng, không thể từ chối message {DeliveryTag}", ea.DeliveryTag);
                        }
                    }
                };

                if (!_consumers.ContainsKey(queueName))
                {
                    _consumers[queueName] = new List<(EventingBasicConsumer Consumer, string ConsumerTag)>();
                }
                
                var consumerTag = _channel.BasicConsume(
                    queue: queueName,
                    autoAck: false,
                    consumer: consumer
                );
                
                _consumers[queueName].Add((consumer, consumerTag));

                _logger.LogInformation("Bắt đầu consumer thứ {ConsumerCount} cho queue: {QueueName}", _consumers[queueName].Count, queueName);
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
                "save_answer_queue",
                "submit_exam_queue",
                "save_exam_queue",
                "student_import_queue" // Thêm queue cho student import
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
                _logger.LogInformation("Đã khai báo queue: {QueueName}", queueName);
            }
        }

        private (ushort prefetchCount, int batchSize, TimeSpan timerInterval) GetQueueConfig(string queueName)
        {
            return queueName switch
            {
                "save_answer_queue" => (100, 1, TimeSpan.FromMilliseconds(50)),
                "submit_exam_queue" => (30, 1, TimeSpan.FromMilliseconds(25)),
                "save_exam_queue" => (30, 1, TimeSpan.FromMilliseconds(25)),
                "student_import_queue" => (5, 1, TimeSpan.FromMilliseconds(500)), // cấu hình cho student import (xử lý chậm hơn)
                _ => (75, 1, TimeSpan.FromMilliseconds(50))
            };
        }

        public void CheckQueueStatus(string queueName)
        {
            try
            {
                if (_channel?.IsOpen ?? false)
                {
                    var queueInfo = _channel.QueueDeclarePassive(queueName);
                                    _logger.LogInformation("Trạng thái queue {QueueName}: Messages={MessageCount}, Consumers={ConsumerCount}", 
                    queueName, queueInfo.MessageCount, queueInfo.ConsumerCount);
                }
                else
                {
                    _logger.LogWarning("Kênh không mở, không thể kiểm tra trạng thái queue");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra trạng thái queue cho {QueueName}", queueName);
            }
        }

        public void Dispose()
        {
            try
            {
                // Đóng tất cả consumers
                foreach (var queueConsumers in _consumers.Values)
                {
                    foreach (var (consumer, consumerTag) in queueConsumers)
                    {
                        if (!string.IsNullOrEmpty(consumerTag))
                        {
                            try
                            {
                                _channel.BasicCancel(consumerTag);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Lỗi khi đóng consumer");
                            }
                        }
                    }
                }
                
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