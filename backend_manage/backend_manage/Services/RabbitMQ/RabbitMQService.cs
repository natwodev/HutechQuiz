using System;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using backend_manage.DTOs;
using backend_manage.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace backend_manage.Services.RabbitMQ
{
    public class RabbitMQService : IRabbitMQService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _queueName = "exam_submission_queue";
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<RabbitMQService> _logger;

        public RabbitMQService(IConfiguration configuration, 
            IServiceScopeFactory serviceScopeFactory,
            ILogger<RabbitMQService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:HostName"],
                UserName = configuration["RabbitMQ:UserName"],
                Password = configuration["RabbitMQ:Password"],
                Port = int.Parse(configuration["RabbitMQ:Port"])
            };

            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(queue: _queueName,
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

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

                _channel.BasicPublish(exchange: "",
                                    routingKey: _queueName,
                                    basicProperties: null,
                                    body: body);

                _logger.LogInformation("Message published to RabbitMQ successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to RabbitMQ");
                throw;
            }
        }

        public void StartConsuming()
        {
            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var examSubmission = JsonConvert.DeserializeObject<ExamSubmissionMessage>(message);

                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        
                        var studentExamSession = await dbContext.StudentExamSessions
                            .FirstOrDefaultAsync(x => x.StudentCode == examSubmission.StudentCode 
                                && x.ShuffledExamPaperId == examSubmission.ShuffledExamPaperId);

                        if (studentExamSession != null)
                        {
                            studentExamSession.Score = examSubmission.Score;
                            studentExamSession.CorrectAnswers = examSubmission.CorrectAnswers;
                            studentExamSession.TotalQuestions = examSubmission.TotalQuestions;
                            studentExamSession.IsCompleted = examSubmission.IsCompleted;
                            studentExamSession.EndTime = examSubmission.EndTime;
                            studentExamSession.StudentAnswersString = examSubmission.StudentAnswersString;

                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Exam submission processed successfully");
                        }
                        else
                        {
                            _logger.LogWarning("StudentExamSession not found for StudentCode: {StudentCode}, ShuffledExamPaperId: {ShuffledExamPaperId}",
                                examSubmission.StudentCode, examSubmission.ShuffledExamPaperId);
                        }
                    }

                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing exam submission message");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(queue: _queueName,
                                autoAck: false,
                                consumer: consumer);

            _logger.LogInformation("Started consuming messages from RabbitMQ");
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
} 