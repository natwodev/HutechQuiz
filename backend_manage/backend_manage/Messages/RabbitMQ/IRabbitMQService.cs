using System;
using System.Threading.Tasks;

namespace backend_manage.Messages.RabbitMQ
{
    public interface IRabbitMqService
    {
        void PublishMessage<T>(string queueName, T message);
        void Subscribe<T>(string queueName, Func<T, Task> onMessage);
        void CheckQueueStatus(string queueName);
        void Dispose();
    }

    public interface IRabbitMqConsumer
    {
        void StartConsuming();
    }
} 