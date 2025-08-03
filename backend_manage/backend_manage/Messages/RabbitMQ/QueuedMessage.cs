namespace backend_manage.Messages.RabbitMQ;

public class QueuedMessage
{
    public string QueueName { get; set; } = default!;
    public object Message { get; set; } = default!;
}