using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using backend_manage.Data;
using backend_manage.DTOs;
using backend_manage.Entities;

namespace backend_manage.Services.RabbitMQ
{
    public class RabbitMQService : IRabbitMQService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _queueName = "exam_submission_queue";
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<RabbitMQService> _logger;

        public RabbitMQService(
            IConfiguration configuration,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<RabbitMQService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;

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

                // Đảm bảo queue tồn tại và có các thuộc tính phù hợp
                _channel.QueueDeclare(
                    queue: _queueName,
                    durable: true, // Queue sẽ tồn tại sau khi restart
                    exclusive: false, // Không chỉ connection này mới được sử dụng
                    autoDelete: false, // Không tự động xóa khi không còn consumer
                    arguments: null
                );

                // Đảm bảo chỉ xử lý một message tại một thời điểm
                _channel.BasicQos(
                    prefetchSize: 0, // Không giới hạn kích thước message
                    prefetchCount: 1, // Chỉ lấy 1 message tại một thời điểm
                    global: false
                );

                _logger.LogInformation("RabbitMQ connection established successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to establish RabbitMQ connection");
                throw;
            }
        }

        public void PublishExamSubmission<T>(T message)
        {
            try
            {
                var json = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true; // Message sẽ được lưu vào disk
                properties.ContentType = "application/json";

                _channel.BasicPublish(
                    exchange: string.Empty,
                    routingKey: _queueName,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation("Message published to RabbitMQ successfully: {Message}", json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to RabbitMQ");
                throw;
            }
        }

        public void StartConsuming()
        {
            try
            {
                var consumer = new EventingBasicConsumer(_channel);

                consumer.Received += (sender, ea) =>
                {
                    var message = string.Empty;
                    try
                    {
                        message = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var examSubmission = JsonConvert.DeserializeObject<ExamSubmissionMessage>(message);

                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                            var studentExamSession = dbContext.StudentExamSessions
                                .FirstOrDefault(x => x.StudentCode == examSubmission.StudentCode
                                    && x.ShuffledExamPaperId == examSubmission.ShuffledExamPaperId);

                            if (studentExamSession != null)
                            {
                                studentExamSession.Score = examSubmission.Score;
                                studentExamSession.CorrectAnswers = examSubmission.CorrectAnswers;
                                studentExamSession.TotalQuestions = examSubmission.TotalQuestions;
                                studentExamSession.IsCompleted = examSubmission.IsCompleted;
                                studentExamSession.EndTime = examSubmission.EndTime;
                                studentExamSession.StudentAnswersString = examSubmission.StudentAnswersString;

                                dbContext.SaveChanges();
                                _logger.LogInformation("Exam submission processed successfully: {Message}", message);
                                
                                // Xác nhận đã xử lý message thành công
                                _channel.BasicAck(ea.DeliveryTag, false);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "StudentExamSession not found. StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}",
                                    examSubmission.StudentCode,
                                    examSubmission.ShuffledExamPaperId
                                );
                                // Reject message và không requeue vì không tìm thấy session
                                _channel.BasicReject(ea.DeliveryTag, false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message: {Message}", message);
                        // Nếu xử lý lỗi, đẩy message vào queue để xử lý lại sau
                        _channel.BasicNack(ea.DeliveryTag, false, true);
                    }
                };

                _channel.BasicConsume(
                    queue: _queueName,
                    autoAck: false, // Không tự động xác nhận, phải xác nhận thủ công
                    consumer: consumer
                );

                _logger.LogInformation("Started consuming messages from RabbitMQ queue: {QueueName}", _queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting RabbitMQ consumer");
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RabbitMQ connections");
            }
        }
    }
} 