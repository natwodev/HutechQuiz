using System.Collections.Concurrent;

namespace backend_manage.Messages.RabbitMQ.FallBack;

public class FallBack<T>
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);

    public void Enqueue(T message)
    {
        _queue.Enqueue(message);
        _signal.Release();
    }

    public async Task<T?> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        _queue.TryDequeue(out var message);
        return message;
    }

    public bool HasMessage => !_queue.IsEmpty;
}

