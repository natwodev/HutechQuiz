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

                _logger.LogInformation("RabbitMQ connection established successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to establish RabbitMQ connection");
                throw;
            }
        }

        public void PublishMessage<T>(string queueName, T message)
        {
            try
            {
                _channel.QueueDeclare(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

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

                _logger.LogInformation("Message published to queue {QueueName}: {Message}", queueName, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to queue {QueueName}", queueName);
                throw;
            }
        }

        public void Subscribe<T>(string queueName, Action<T> onMessage)
        {
            try
            {
                _channel.QueueDeclare(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                _channel.BasicQos(0, 1, false);

                var consumer = new EventingBasicConsumer(_channel);

                consumer.Received += (sender, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var data = JsonConvert.DeserializeObject<T>(message);

                        onMessage(data);

                        _channel.BasicAck(ea.DeliveryTag, false);
                        _logger.LogInformation("Message processed from queue {QueueName}: {Message}", queueName, message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
                        _channel.BasicNack(ea.DeliveryTag, false, true);
                    }
                };

                var consumerTag = _channel.BasicConsume(
                    queue: queueName,
                    autoAck: false,
                    consumer: consumer
                );

                _consumers[queueName] = consumer;

                _logger.LogInformation("Started consuming messages from queue: {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to queue {QueueName}", queueName);
                throw;
            }
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

                _logger.LogInformation("RabbitMQ connections closed and disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RabbitMQ connections");
            }
        }
    }
} 