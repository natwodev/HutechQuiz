namespace backend_manage.Messages.RabbitMQ;

public interface IRabbitMqService
{
    void Publish<T>(string queueName, T message);
    void Subscribe<T>(string queueName, Func<T, Task> onMessage);
    void CheckQueueStatus(string queueName); // Optional
}