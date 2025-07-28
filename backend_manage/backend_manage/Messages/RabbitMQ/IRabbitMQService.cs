namespace backend_manage.Messages.RabbitMQ
{
    public interface IRabbitMqService
    {
        void PublishMessage<T>(string queueName, T message);
        void Subscribe<T>(string queueName, Action<T> onMessage);
    }
} 