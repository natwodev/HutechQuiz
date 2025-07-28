namespace backend_manage.Messages.RabbitMQ
{
    public interface IRabbitMQService
    {
        void PublishMessage<T>(string queueName, T message);
        void Subscribe<T>(string queueName, Action<T> onMessage);
    }
} 