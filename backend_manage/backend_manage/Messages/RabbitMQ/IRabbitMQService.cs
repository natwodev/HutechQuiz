namespace backend_manage.Messages.RabbitMQ;

public interface IRabbitMqService
{
    bool IsConnected { get; }
    void TryReconnect();
    void Publish<T>(string queueName, T message);
    void Subscribe<T>(string queueName, Func<T, Task> onMessage);
    void CheckQueueStatus(string queueName);
}