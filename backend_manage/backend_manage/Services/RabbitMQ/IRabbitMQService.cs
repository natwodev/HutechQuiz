namespace backend_manage.Services.RabbitMQ
{
    public interface IRabbitMQService
    {
        void PublishExamSubmission<T>(T message);
        void StartConsuming();
    }
} 